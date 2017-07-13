﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Entities.Wallets;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Core.Services;
using Lykke.Job.Pay.ProcessRequests.Services.RabbitMq;
using Lykke.Pay.Service.GenerateAddress.Client;
using Lykke.Pay.Service.GenerateAddress.Client.Models;
using Lykke.Pay.Service.StoreRequest.Client.Models;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    public class BalanceChangeHandler : IBalanceChangeHandler
    {
        private readonly ILog _log;
        private readonly AppSettings.ProcessRequestSettings _settings;
        private RabbitMqSubscriber<WalletMqModel> _subscriber;
        private readonly ILykkePayServiceGenerateAddressMicroService _walletClient;

        public BalanceChangeHandler(AppSettings.ProcessRequestSettings settings, ILog log,
            ILykkePayServiceGenerateAddressMicroService walletClient)
        {
            _log = log;
            _settings = settings;
            _walletClient = walletClient;
        }
        public void Start()
        {
            try
            {
                _subscriber = new RabbitMqSubscriber<WalletMqModel>(new RabbitMqSubscriberSettings
                    {
                        ConnectionString = _settings.WalletBroadcastRabbit.ConnectionString,
                        QueueName = $"{_settings.WalletBroadcastRabbit.ExchangeName}.balancehandler",
                        ExchangeName = _settings.WalletBroadcastRabbit.ExchangeName,
                        IsDurable = true
                    })
                    .SetMessageDeserializer(new JsonMessageDeserializer<WalletMqModel>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(ProcessWalletAsync)
                    .SetLogger(_log)
                    .Start();
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(ProcessRequest.ComponentName, null, null, ex).Wait();
                throw;
            }
        }

        private async Task ProcessWalletAsync(WalletMqModel wallet)
        {
            try
            {
                var param = new List<WallerChangeRequest>(
                    from w in wallet.Wallets
                    select new WallerChangeRequest
                    {
                        Assert = "BTC",
                        DeltaAmount = w.AmountChange,
                        WalletAddress = w.Address
                    });

               await _walletClient.ApiWalletPostWithHttpMessagesAsync(param);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(ProcessRequest.ComponentName, null, null, ex);
            }
        }

        public void Dispose()
        {
            _subscriber.Stop();
        }
    }
}
