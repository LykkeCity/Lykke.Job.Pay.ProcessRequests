using System.Threading.Tasks;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.RequestFactory;

namespace Lykke.Job.Pay.ProcessRequests.Services.RequestFactory
{
    class ExchangeTransferRequestHandler : RequestHandler
    {
        public override async Task Handle()
        {
            await Task.Delay(10);
        }

        public ExchangeTransferRequestHandler(IMerchantPayRequest payRequest, AppSettings.ProcessRequestSettings settings) : base(payRequest, settings)
        {
        }
    }
}
