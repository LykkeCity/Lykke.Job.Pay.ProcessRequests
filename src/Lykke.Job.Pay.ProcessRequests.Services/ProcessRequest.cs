using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.AzureRepositories;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.RequestFactory;
using Lykke.Pay.Service.StoreRequest.Client;
using Newtonsoft.Json;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    // NOTE: This is job service class example
    public class ProcessRequest : IProcessRequest
    {
        private static readonly string ComponentName = "Lykke.Job.Pay.ProcessRequests";
        private readonly ILog _log;
        private readonly AppSettings.ProcessRequestSettings _settings;
        private readonly ILykkePayServiceStoreRequestMicroService _storeClient;


        public ProcessRequest(AppSettings.ProcessRequestSettings settings, ILog log, ILykkePayServiceStoreRequestMicroService storeClient)
        {
            _log = log;
            _storeClient = storeClient;
            _settings = settings;
        }
        public async Task ProcessAsync()
        {

            await _log.WriteInfoAsync(ComponentName, "Process started", null,
                $"ProcessAsync rised");

           
            var response = await _storeClient.ApiStoreGetWithHttpMessagesAsync();
            var json = await response.Response.Content.ReadAsStringAsync();
            var requests = JsonConvert.DeserializeObject<List<MerchantPayRequest>>(json);

            foreach (var request in requests)
            {
                var handler = RequestHandler.Create(request, _settings);
                await handler.Handle();
            }

        }

        
        
    }
}