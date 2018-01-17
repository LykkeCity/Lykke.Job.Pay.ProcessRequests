using System;
using System.Net;
using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bitcoint.Api.Client;
using Common.Log;
using Lykke.AzureRepositories;
using Lykke.AzureRepositories.Azure.Tables;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Job.Pay.ProcessRequests.Services;
using Lykke.Pay.Service.GenerateAddress.Client;
using Lykke.Pay.Service.StoreRequest.Client;
using Lykke.Pay.Service.Wallets.Client;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.RPC;

namespace Lykke.Job.Pay.ProcessRequests.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(AppSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.ProcessRequestJob)
                .SingleInstance();

            builder.RegisterInstance(_settings.NinjaServiceClient)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(TimeSpan.FromSeconds(30)));

            var bitcoinAggRepository = new BitcoinAggRepository(
                        new AzureTableStorage<BitcoinAggEntity>(
                            _settings.ProcessRequestJob.Db.MerchantWalletConnectionString, "BitcoinAgg",
                            null),
                        new AzureTableStorage<BitcoinHeightEntity>(
                            _settings.ProcessRequestJob.Db.MerchantWalletConnectionString, "BitcoinHeight",
                            null));
            builder.RegisterInstance(bitcoinAggRepository)
                .As<IBitcoinAggRepository>()
                .SingleInstance();

            var merchantPayRequestRepository =
            new MerchantPayRequestRepository(
                new AzureTableStorage<MerchantPayRequest>(_settings.ProcessRequestJob.Db.MerchantWalletConnectionString, "MerchantPayRequest", null));

            builder.RegisterInstance(merchantPayRequestRepository)
                .As<IMerchantPayRequestRepository>()
                .SingleInstance();

            var merchantOrderRequestRepository =
                new MerchantOrderRequestRepository(
                    new AzureTableStorage<MerchantOrderRequest>(_settings.ProcessRequestJob.Db.MerchantWalletConnectionString, "MerchantOrderRequest", null));

            builder.RegisterInstance(merchantOrderRequestRepository)
                .As<IMerchantOrderRequestRepository>()
                .SingleInstance();

            builder.RegisterType<BitcoinApi>()
                .As<IBitcoinApi>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(new Uri(_settings.BitcoinApiClient.ServiceUrl)));

            builder.RegisterType<LykkePayServiceStoreRequestMicroService>()
                .As<ILykkePayServiceStoreRequestMicroService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(new Uri(_settings.ProcessRequestJob.Services.LykkePayServiceStoreRequestMicroService)));

            builder.RegisterType<LykkePayServiceGenerateAddressMicroService>()
                .As<ILykkePayServiceGenerateAddressMicroService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(new Uri(_settings.ProcessRequestJob.Services.LykkePayServiceGenerateAddressMicroService)));

            builder.RegisterType<ProcessRequest>()
                .As<IProcessRequest>()
                .SingleInstance();

            var client = new RPCClient(
                new NetworkCredential(_settings.ProcessRequestJob.Rpc.UserName,
                    _settings.ProcessRequestJob.Rpc.Password),
                new Uri(_settings.ProcessRequestJob.Rpc.Url), Network.GetNetwork(_settings.ProcessRequestJob.Rpc.Network));
            builder.RegisterInstance(client)
                .As<RPCClient>()
                .SingleInstance();

            //builder.RegisterType<BalanceChangeHandler>()
            //    .As<IBalanceChangeHandler>()
            //    .As<IStartable>()
            //    .SingleInstance();

            builder.Populate(_services);

        }


    }
}