using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bitcoint.Api.Client;
using Common.Log;
using Lykke.AzureRepositories;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Services.RequestFactory;
using Lykke.Pay.Service.GenerateAddress.Client;
using Lykke.Pay.Service.StoreRequest.Client;
using NBitcoin.RPC;
using Newtonsoft.Json;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    // NOTE: This is job service class example
    public class ProcessRequest : IProcessRequest
    {
        public static readonly string ComponentName = "Lykke.Job.Pay.ProcessRequests";
        private readonly ILog _log;
        private readonly AppSettings.ProcessRequestSettings _settings;
        private readonly ILykkePayServiceStoreRequestMicroService _storeClient;

        private readonly IBitcoinAggRepository _bitcoinRepo;
        private readonly IMerchantPayRequestRepository _merchantPayRequestRepository;
        private readonly IBitcoinApi _bitcoinApi;
        private readonly IMerchantOrderRequestRepository _merchantOrderRequestRepository;
        private readonly ILykkePayServiceGenerateAddressMicroService _generateAddressMicroService;
        private readonly IBitcoinAggRepository _bitcoinAggRepository;
        private readonly RPCClient _rpcClient;

        public ProcessRequest(AppSettings.ProcessRequestSettings settings, ILog log, ILykkePayServiceStoreRequestMicroService storeClient,
            IBitcoinAggRepository bitcoinRepo, IMerchantPayRequestRepository merchantPayRequestRepository, IBitcoinApi bitcoinApi,
            IMerchantOrderRequestRepository merchantOrderRequestRepository, ILykkePayServiceGenerateAddressMicroService generateAddressMicroService,
            IBitcoinAggRepository bitcoinAggRepository, RPCClient rpcClient)
        {
            _log = log;
            _storeClient = storeClient;
            _settings = settings;
            _bitcoinRepo = bitcoinRepo;
            _merchantPayRequestRepository = merchantPayRequestRepository;
            _bitcoinApi = bitcoinApi;
            _merchantOrderRequestRepository = merchantOrderRequestRepository;
            _generateAddressMicroService = generateAddressMicroService;
            _bitcoinAggRepository = bitcoinAggRepository;
            _rpcClient = rpcClient;
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
                var handler = RequestHandler.Create(request, _settings, _bitcoinRepo, _merchantPayRequestRepository, _bitcoinApi);
                await handler.Handle();
            }

            var orderStr = await _storeClient.ApiStoreOrderGetWithHttpMessagesAsync();
            json = await orderStr.Response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<MerchantOrderRequest>>(json);
            var addresses = from o in orders
                group o by o.SourceAddress
                into go
                select new MerchantOrderAddress(go.Key, go.OrderBy(o=>o.TransactionWaitingTime.GetRepoDateTime()).ToList());
            foreach (var address in addresses)
            {
                var handler = RequestHandler.Create(address, _settings, _merchantOrderRequestRepository, _generateAddressMicroService, _log,
                    _bitcoinAggRepository, _rpcClient);
                await handler.Handle();
            }


        }

        
        
    }
}