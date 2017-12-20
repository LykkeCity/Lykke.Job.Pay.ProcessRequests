using System.Threading.Tasks;
using Bitcoint.Api.Client;
using Common.Log;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Pay.Service.GenerateAddress.Client;
using NBitcoin.RPC;

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
                case MerchantPayRequestType.Purchase:
                   // return new PurchaseRequestHandler(payRequest, settings);
                case MerchantPayRequestType.ExchangeTransfer:
                case MerchantPayRequestType.Transfer:
                    return new TransferRequestHandler(payRequest, settings, bitcoinRepo, merchantPayRequestRepository, bitcoinApi);
            }

            return null;
        }

        public abstract Task Handle();

        internal static IRequestHandler Create(MerchantOrderAddress address, AppSettings.ProcessRequestSettings settings,
            IMerchantOrderRequestRepository merchantOrderRequestRepository, ILykkePayServiceGenerateAddressMicroService generateAddressMicroService, ILog log,
            IBitcoinAggRepository bitcoinAggRepository, RPCClient rpcClient)
        {
            return new OrderRequestHandler(address, settings, merchantOrderRequestRepository, generateAddressMicroService, log, bitcoinAggRepository, rpcClient);
        }
    }
}
