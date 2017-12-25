using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bitcoint.Api.Client;
using Common.Log;
using Lykke.Common.Entities.Pay;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Pay.Common;
using Lykke.Pay.Service.GenerateAddress.Client;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json;

namespace Lykke.Job.Pay.ProcessRequests.Services.RequestFactory
{
    public class OrderRequestHandler : IRequestHandler
    {
        private readonly MerchantOrderAddress _address;
        private readonly AppSettings.ProcessRequestSettings _settings;
        private readonly IMerchantOrderRequestRepository _merchantOrderRequestRepository;
        private readonly ILykkePayServiceGenerateAddressMicroService _generateAddressMicroService;
        private readonly ILog _log;
        private readonly string _contextName = "Order Request handle";
        private readonly IBitcoinAggRepository _bitcoinAggRepository;
        private readonly RPCClient _rpcClient;

        public OrderRequestHandler(MerchantOrderAddress address, AppSettings.ProcessRequestSettings settings, IMerchantOrderRequestRepository merchantOrderRequestRepository, ILykkePayServiceGenerateAddressMicroService generateAddressMicroService, ILog log, IBitcoinAggRepository bitcoinAggRepository, RPCClient rpcClient)
        {
            _address = address;
            _settings = settings;
            _merchantOrderRequestRepository = merchantOrderRequestRepository;
            _generateAddressMicroService = generateAddressMicroService;
            _log = log;
            _bitcoinAggRepository = bitcoinAggRepository;
            _rpcClient = rpcClient;
        }

        public async Task Handle()
        {
            try
            {
                if (_address.Orders.Any(o => o.MerchantPayRequestStatus != MerchantPayRequestStatus.InProgress &&
                                             o.MerchantPayRequestStatus != MerchantPayRequestStatus.New))
                {
                    return;
                }
                await _log.WriteInfoAsync($"Handle {_address.Address} orders", _contextName, null);
                if (_address.Orders.Any(o => o.MerchantPayRequestStatus == MerchantPayRequestStatus.New))
                {
                    var amount = await GetWalletAmount();
                    if (Math.Abs(amount) < 0.00000001)
                    {
                        return;
                    }

                    await _log.WriteInfoAsync($"Handle {_address.Address} orders", _contextName, null);

                    var transactions =
                        (await _bitcoinAggRepository.GetWalletTransactionsAsync(_address.Address)).ToList();
                    var transaction = transactions.FirstOrDefault();

                    bool isError = true;
                    if (transaction != null && transactions.Count == 1)
                    {
                        await _log.WriteInfoAsync(
                            $"Transaction {transaction.TransactionId} was found in {transaction.BlockNumber} block",
                            _contextName, null);
                        var block = await _rpcClient.GetBlockAsync(transaction.BlockNumber);
                        if (block != null)
                        {
                            var order = _address.Orders.FirstOrDefault(o => o.TransactionWaitingTime.GetRepoDateTime() >
                                                                            block.Header.BlockTime.LocalDateTime);
                            if (order != null)
                            {
                                await _log.WriteInfoAsync($"Handle {order.OrderId} order", _contextName, null);
                                order.TransactionId = transaction.TransactionId;
                                order.TransactionDetectionTime = block.Header.BlockTime.LocalDateTime.RepoDateStr();
                                if (order.Amount.BtcEqualTo(transaction.Amount))
                                {
                                    await _log.WriteInfoAsync("Amounts are equal", _contextName, null);
                                    order.TransactionStatus = InvoiceStatus.InProgress.ToString();
                                    isError = false;
                                }
                                else
                                {
                                    order.TransactionStatus =
                                    (order.Amount < transaction.Amount
                                        ? InvoiceStatus.Overpaid
                                        : InvoiceStatus.Underpaid).ToString();
                                }

                                order.MerchantPayRequestStatus =
                                    isError ? MerchantPayRequestStatus.Failed : MerchantPayRequestStatus.InProgress;
                                order.MerchantPayRequestNotification |=
                                    isError
                                        ? MerchantPayRequestNotification.Error
                                        : MerchantPayRequestNotification.InProgress;
                                order.ETag = "*";
                                await _merchantOrderRequestRepository.SaveRequestAsync(order);
                            }

                        }
                    }
                    else
                    {
                        var order = _address.Orders.First();
                        if (_address.Orders.First().TransactionWaitingTime.GetRepoDateTime() < DateTime.Now)
                        {
                            order.TransactionStatus = InvoiceStatus.LatePaid.ToString();
                            order.MerchantPayRequestStatus = MerchantPayRequestStatus.Failed;
                            order.ETag = "*";
                            await _merchantOrderRequestRepository.SaveRequestAsync(order);
                        }
                    }
                }
                else
                {
                    var transactions =
                        (await _bitcoinAggRepository.GetWalletTransactionsAsync(_address.Address)).ToList();
                    var transaction = transactions.FirstOrDefault();

                    if (transaction != null && transactions.Count == 1)
                    {

                        await _log.WriteInfoAsync(
                            $"Transaction {transaction.TransactionId} was found in {transaction.BlockNumber} block",
                            _contextName, null);

                        var blocks = _rpcClient.GetBlockCount();
                        var order = _address.Orders.First(
                            o => o.MerchantPayRequestStatus == MerchantPayRequestStatus.InProgress);
                        if (blocks - transaction.BlockNumber >= _settings.NumberOfConfirmations)
                        {
                            
                            order.TransactionStatus = InvoiceStatus.Paid.ToString();
                        }
                       
                        order.MerchantPayRequestStatus = MerchantPayRequestStatus.Completed;
                        order.MerchantPayRequestNotification |= MerchantPayRequestNotification.Success;
                    }
                }
                

            }
            catch (Exception e)
            {
                
                await _log.WriteErrorAsync($"Can't proceed {_address.Address} orders. Exception.", _contextName, e);
            }
            

           
        }

     

        private async Task<double> GetWalletAmount()
        {
            var walletResult = await _generateAddressMicroService.ApiWalletByMerchantIdGetWithHttpMessagesAsync(_address.Orders.First().MerchantId);
            try
            {
                var res = (from w in walletResult.Body
                    where _address.Address.Equals(w.WalletAddress, StringComparison.CurrentCultureIgnoreCase)
                    select w.Amount).First();
                return res.Value;
            }
            catch
            {
                return 0;
            }
            

            
        }

      
    }
}