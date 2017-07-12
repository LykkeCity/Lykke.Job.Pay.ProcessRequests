using System.Threading.Tasks;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.RequestFactory;

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

        public static IRequestHandler Create(IMerchantPayRequest payRequest, AppSettings.ProcessRequestSettings settings)
        {
            switch (payRequest.MerchantPayRequestType)
            {
                case MerchantPayRequestType.ExchangeTransfer:
                    return new ExchangeTransferRequestHandler(payRequest, settings);
                case MerchantPayRequestType.Purchase:
                    return new PurchaseRequestHandler(payRequest, settings);
                case MerchantPayRequestType.Transfer:
                    return new TransferRequestHandler(payRequest, settings);
            }

            return null;
        }

        public abstract Task Handle();

    }
}
