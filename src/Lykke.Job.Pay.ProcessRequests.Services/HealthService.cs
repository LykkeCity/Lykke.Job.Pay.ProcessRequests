using System;
using Lykke.Job.Pay.ProcessRequests.Core.Services;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    public class HealthService : IHealthService
    {
        // NOTE: These are example properties
        public DateTime LastPrServiceStartedMoment { get; private set; }
        public TimeSpan LastPrServiceDuration { get; private set; }
        public TimeSpan MaxHealthyPrServiceDuration { get; }

        // NOTE: These are example properties
        private bool WasLastPrServiceFailed { get; set; }
        private bool WasLastPrServiceCompleted { get; set; }
        private bool WasClientsPrServiceEverStarted { get; set; }

        // NOTE: When you change parameters, don't forget to look in to JobModule

        public HealthService(TimeSpan maxHealthyPrServiceDuration)
        {
            MaxHealthyPrServiceDuration = maxHealthyPrServiceDuration;
        }

        // NOTE: This method probably would stay in the real job, but will be modified
        public string GetHealthViolationMessage()
        {
            if (WasLastPrServiceFailed)
            {
                return "Last PrService was failed";
            }

            if (!WasLastPrServiceCompleted && !WasLastPrServiceFailed && !WasClientsPrServiceEverStarted)
            {
                return "Waiting for first PrService execution started";
            }

            if (!WasLastPrServiceCompleted && !WasLastPrServiceFailed && WasClientsPrServiceEverStarted)
            {
                return $"Waiting {DateTime.UtcNow - LastPrServiceStartedMoment} for first PrService execution completed";
            }

            if (LastPrServiceDuration > MaxHealthyPrServiceDuration)
            {
                return $"Last PrService was lasted for {LastPrServiceDuration}, which is too long";
            }
            return null;
        }

        // NOTE: These are example methods
        public void TracePrServiceStarted()
        {
            LastPrServiceStartedMoment = DateTime.UtcNow;
            WasClientsPrServiceEverStarted = true;
        }

        public void TracePrServiceCompleted()
        {
            LastPrServiceDuration = DateTime.UtcNow - LastPrServiceStartedMoment;
            WasLastPrServiceCompleted = true;
            WasLastPrServiceFailed = false;
        }

        public void TracePrServiceFailed()
        {
            WasLastPrServiceCompleted = false;
            WasLastPrServiceFailed = true;
        }

        public void TraceBooStarted()
        {
            // TODO: See PrService
        }

        public void TraceBooCompleted()
        {
            // TODO: See PrService
        }

        public void TraceBooFailed()
        {
            // TODO: See PrService
        }
    }
}