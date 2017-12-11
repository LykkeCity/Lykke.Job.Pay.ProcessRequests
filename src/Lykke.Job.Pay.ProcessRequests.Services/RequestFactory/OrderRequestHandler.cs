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
using Lykke.Pay.Service.GenerateAddress.Client;
using Newtonsoft.Json;

namespace Lykke.Job.Pay.ProcessRequests.Services.RequestFactory
{
    public class OrderRequestHandler : IRequestHandler
    {
        private readonly IMerchantOrderRequest _order;
        private readonly AppSettings.ProcessRequestSettings _settings;
        private readonly IMerchantOrderRequestRepository _merchantOrderRequestRepository;
        private readonly ILykkePayServiceGenerateAddressMicroService _generateAddressMicroService;
        private readonly ILog _log;
        private readonly string _contextName = "Order Request handle";

        public OrderRequestHandler(IMerchantOrderRequest order, AppSettings.ProcessRequestSettings settings, IMerchantOrderRequestRepository merchantOrderRequestRepository, ILykkePayServiceGenerateAddressMicroService generateAddressMicroService, ILog log)
        {
            _order = order;
            _settings = settings;
            _merchantOrderRequestRepository = merchantOrderRequestRepository;
            _generateAddressMicroService = generateAddressMicroService;
            _log = log;
        }

        public async Task Handle()
        {
            try
            {
                var amount = await GetWalletAmount();
                if (amount > 0)
                {
                    return;
                }

                var orderDate = ParseOrderDate();
                if (amount.BtcEqualTo(_order.Amount) && orderDate.ToUniversalTime() < DateTime.UtcNow)
                {
                    _order.MerchantPayRequestNotification |= MerchantPayRequestNotification.Success;
                    _order.MerchantPayRequestStatus = MerchantPayRequestStatus.Completed;
                    await _log.WriteInfoAsync($"Proceed  {_order.RequestId} order. Success", _contextName, null);
                }
                else
                {
                    _order.MerchantPayRequestNotification |= MerchantPayRequestNotification.Error;
                    _order.MerchantPayRequestStatus = MerchantPayRequestStatus.Failed;
                    await _log.WriteInfoAsync($"Proceed  {_order.RequestId} order. Error with amount or date", _contextName, null);
                }
            }
            catch (Exception e)
            {
                _order.MerchantPayRequestNotification |= MerchantPayRequestNotification.Error;
                _order.MerchantPayRequestStatus = MerchantPayRequestStatus.Failed;
                await _log.WriteErrorAsync($"Can't proceed {_order.RequestId} order. Exception.", _contextName, e);
            }
            

            await _merchantOrderRequestRepository.SaveRequestAsync(_order);
        }

        private DateTime ParseOrderDate()
        {
            return DateTime.Parse(_order.TransactionWaitingTime, CultureInfo.InvariantCulture);
        }

        private async Task<double> GetWalletAmount()
        {
            var walletResult = await _generateAddressMicroService.ApiWalletByMerchantIdGetWithHttpMessagesAsync(_order.MerchantId);
            var res = (from w in walletResult.Body
                where _order.SourceAddress.Equals(w.WalletAddress, StringComparison.CurrentCultureIgnoreCase)
                select w.Amount).First();

            return res.Value;
        }

      
    }
}