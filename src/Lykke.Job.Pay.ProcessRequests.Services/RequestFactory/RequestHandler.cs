﻿using System.Threading.Tasks;
using Bitcoint.Api.Client;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;

namespace Lykke.Job.Pay.ProcessRequests.Services.RequestFactory
{
    public abstract class RequestHandler : IRequestHandler
    {
        protected IMerchantPayRequest MerchantPayRequest { get; set; }
        protected AppSettings.ProcessRequestSettings Settings { get; set; }


        protected RequestHandler(IMerchantPayRequest payRequest, AppSettings.ProcessRequestSettings settings)
        {
            MerchantPayRequest = payRequest;
            Settings = settings;
        }

        public static IRequestHandler Create(IMerchantPayRequest payRequest, AppSettings.ProcessRequestSettings settings,
            IBitcoinAggRepository bitcoinRepo, IMerchantPayRequestRepository merchantPayRequestRepository, IBitcoinApi bitcoinApi)
        {
            switch (payRequest.MerchantPayRequestType)
            {
                case MerchantPayRequestType.ExchangeTransfer:
                    return new ExchangeTransferRequestHandler(payRequest, settings);
                case MerchantPayRequestType.Purchase:
                    return new PurchaseRequestHandler(payRequest, settings);
                case MerchantPayRequestType.Transfer:
                    return new TransferRequestHandler(payRequest, settings, bitcoinRepo, merchantPayRequestRepository, bitcoinApi);
            }

            return null;
        }

        public abstract Task Handle();

    }
}
