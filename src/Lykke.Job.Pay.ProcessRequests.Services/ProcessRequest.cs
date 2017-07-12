using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.AzureRepositories;
using Lykke.Common.Entities.Wallets;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Pay.Service.Wallets.Client;
using Lykke.Pay.Service.Wallets.Client.Models;
using NBitcoin;
using NBitcoin.RPC;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    // NOTE: This is job service class example
    public class ProcessRequest : IProcessRequest
    {
        private static readonly string ComponentName = "Lykke.Job.Pay.ProcessRequests";
        private readonly AppSettings.ProcessRequestSettings _settings;
        private readonly ILog _log;



        public ProcessRequest(AppSettings.ProcessRequestSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }
        public async Task ProcessAsync()
        {

            await _log.WriteInfoAsync(ComponentName, "Process started", null,
                $"ProcessAsync rised");
            
        }

        
        
    }
}