using System.Threading.Tasks;

namespace Lykke.Job.Pay.ProcessRequests.Services.RequestFactory
{
    public interface IRequestHandler
    {
        Task Handle();
    }
}
