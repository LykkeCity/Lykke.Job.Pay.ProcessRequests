using System;
using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.AzureRepositories;
using Lykke.AzureRepositories.Azure.Tables;
using Lykke.Core;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Job.Pay.ProcessRequests.Services;
using Lykke.Pay.Service.Wallets.Client;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin.RPC;

namespace Lykke.Job.Pay.ProcessRequests.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings.ProcessRequestSettings _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(AppSettings.ProcessRequestSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings)
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
                            _settings.Db.MerchantWalletConnectionString, "BitcoinAgg",
                            null),
                        new AzureTableStorage<BitcoinHeightEntity>(
                            _settings.Db.MerchantWalletConnectionString, "BitcoinHeight",
                            null));
            builder.RegisterInstance(bitcoinAggRepository)
                .As<IBitcoinAggRepository>()
                .SingleInstance();

            //var client = new RPCClient(
            //    new NetworkCredential(_settings.Rpc.UserName,
            //        _settings.Rpc.Password),
            //    new Uri(_settings.Rpc.Url));
            //builder.RegisterInstance(client)
            //    .As<RPCClient>()
            //    .SingleInstance();

            //builder.RegisterType<PayWalletservice>()
            //    .As<IPayWalletservice>()
            //    .SingleInstance()
            //    .WithParameter(TypedParameter.From(new Uri(_settings.Services.PayWalletServiceUrl))); ;

            //builder.RegisterType<BitcoinBroadcast>()
            //    .As<IBitcoinBroadcast>()
            //    .As<IStartable>()
            //    .SingleInstance();

            builder.RegisterType<ProcessRequest>()
                .As<IProcessRequest>()
                .SingleInstance();



           

        builder.Populate(_services);
        }

        private readonly IBitcoinAggRepository _bitcoinAggRepository;


    }
}