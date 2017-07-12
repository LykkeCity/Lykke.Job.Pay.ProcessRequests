using System;

namespace Lykke.Job.Pay.ProcessRequests.Core.Services
{
    public interface IHealthService
    {
        // NOTE: These are example properties
        DateTime LastPrServiceStartedMoment { get; }
        TimeSpan LastPrServiceDuration { get; }
        TimeSpan MaxHealthyPrServiceDuration { get; }

        // NOTE: This method probably would stay in the real job, but will be modified
        string GetHealthViolationMessage();

        // NOTE: These are example methods
        void TracePrServiceStarted();
        void TracePrServiceCompleted();
        void TracePrServiceFailed();
       
    }
}