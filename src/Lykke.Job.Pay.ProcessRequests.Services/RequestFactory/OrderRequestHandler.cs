using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bitcoint.Api.Client;
using Common.Log;
using Lykke.Common.Entities.Pay;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
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
        private readonly NinjaServiceClient _ninjaSettings;
        private readonly IMerchantOrderRequestRepository _merchantOrderRequestRepository;
        private readonly ILykkePayServiceGenerateAddressMicroService _generateAddressMicroService;
        private readonly ILog _log;
        private readonly string _contextName = "Order Request handle";
        private readonly IBitcoinAggRepository _bitcoinAggRepository;
        private readonly RPCClient _rpcClient;

        public OrderRequestHandler(MerchantOrderAddress address, AppSettings.ProcessRequestSettings settings, IMerchantOrderRequestRepository merchantOrderRequestRepository, ILykkePayServiceGenerateAddressMicroService generateAddressMicroService, ILog log, IBitcoinAggRepository bitcoinAggRepository, RPCClient rpcClient, NinjaServiceClient ninjaSettings)
        {
            _address = address;
            _settings = settings;
            _ninjaSettings = ninjaSettings;
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
                if (_address.Orders.All(o => o.MerchantPayRequestStatus == MerchantPayRequestStatus.New))
                {
                    var addressInfo = await GetAddressInfo();
                    if (addressInfo == null)
                    {
                        return;
                    }

                    var coinOperation = addressInfo.Operations.Select(op => op.ReceivedCoins.FirstOrDefault(rc => _address.Address.Equals(rc.Address))).FirstOrDefault();
                    var operation = addressInfo.Operations.FirstOrDefault(op => op.ReceivedCoins.Any(rc => rc == coinOperation));
                    if (coinOperation == null || operation == null)
                    {
                        return;
                    }
                    var amount = coinOperation.Value / Math.Pow(10, 8);
                    if (Math.Abs(amount) < 0.00000001)
                    {
                        return;
                    }

                    await _log.WriteInfoAsync($"Handle {_address.Address} orders", _contextName, null);


                    var order = _address.Orders.FirstOrDefault(o => o.TransactionWaitingTime.GetRepoDateTime() >
                                                                    operation.FirstSeen.ToLocalTime());
                    var isError = true;
                    if (order != null)
                    {
                        await _log.WriteInfoAsync($"Handle {order.OrderId} order", _contextName, null);
                        order.TransactionId = coinOperation.TransactionId;
                        order.TransactionDetectionTime = operation.FirstSeen.ToLocalTime().RepoDateStr();
                        if (order.Amount.BtcEqualTo(amount))
                        {
                            await _log.WriteInfoAsync("Amounts are equal", _contextName, null);
                            order.TransactionStatus = InvoiceStatus.InProgress.ToString();
                            isError = false;
                        }
                        else
                        {
                            order.TransactionStatus =
                            (order.Amount < amount
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
                    else //LPDEV - 60
                    {
                        order = _address.Orders.First();
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
                            order.MerchantPayRequestStatus = MerchantPayRequestStatus.Completed;
                            order.MerchantPayRequestNotification |= MerchantPayRequestNotification.Success;
                            order.ETag = "*";
                            await _merchantOrderRequestRepository.SaveRequestAsync(order);
                        }


                    }
                }


            }
            catch (Exception e)
            {

                await _log.WriteErrorAsync($"Can't proceed {_address.Address} orders. Exception.", _contextName, e);
            }



        }

        private async Task<NinjaAddressResult> GetAddressInfo()
        {
            var requestUrl = $"{(_ninjaSettings.ServiceUrl).TrimEnd(@"\/".ToCharArray())}/balances/{_address.Address}";
            var nResponse = await new HttpClient().GetAsync(requestUrl);
            if (nResponse.StatusCode != HttpStatusCode.OK)
            {
                await _log.WriteWarningAsync("Getting Address Info",
                    $"Can't get information from Ninja by {requestUrl}", $"Status code returns {nResponse.StatusCode}");
                return null;
            }

            var result = JsonConvert.DeserializeObject<NinjaAddressResult>(await nResponse.Content.ReadAsStringAsync());
            return result;
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