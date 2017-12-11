using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Job.Pay.ProcessRequests.TriggerHandlers
{
    // NOTE: This is the trigger handlers class example
    public class GeneralHandlers
    {
        private readonly IProcessRequest _processRequest;
        private readonly IHealthService _healthService;
        private readonly ILog _log;

        // NOTE: The object is instantiated using DI container, so registered dependencies are injects well
        public GeneralHandlers(
            IProcessRequest processRequest,
            IHealthService healthService,
            ILog log)
        {
            _processRequest = processRequest;
            _healthService = healthService;
            _log = log;
        }

        [TimerTrigger("00:00:10")]
        public async Task TimeTriggeredHandler()
        {
            try
            {
                _healthService.TracePrServiceStarted();

                await _processRequest.ProcessAsync();

                _healthService.TracePrServiceCompleted();
            }
            catch(Exception e)
            {
                await _log.WriteErrorAsync(nameof(GeneralHandlers), nameof(TimeTriggeredHandler), e);

                _healthService.TracePrServiceFailed();
            }
        }
    }
}