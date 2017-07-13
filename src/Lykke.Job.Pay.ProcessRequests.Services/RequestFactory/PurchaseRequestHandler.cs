using System.Threading.Tasks;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;

namespace Lykke.Job.Pay.ProcessRequests.Services.RequestFactory
{
    class PurchaseRequestHandler : RequestHandler
    {
        public override async Task Handle()
        {
            await Task.Delay(10);
        }

        public PurchaseRequestHandler(IMerchantPayRequest payRequest, AppSettings.ProcessRequestSettings settings) : base(payRequest, settings)
        {
        }
    }
}