namespace Lykke.Job.Pay.ProcessRequests.Core
{
    public class AppSettings
    {
        public ProcessRequestSettings ProcessRequestJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }

        public class ProcessRequestSettings
        {
            public DbSettings Db { get; set; }
            public ServicesSettings Services { get; set; }
            public WalletBradcastRabbitSettings WalletBroadcastRabbit { get; set; }
        }

        public class DbSettings
        {
            public string LogsConnString { get; set; }
            public string MerchantWalletConnectionString { get; set; }
        }

        public class SlackNotificationsSettings
        {
            public AzureQueueSettings AzureQueue { get; set; }

            public int ThrottlingLimitSeconds { get; set; }
        }

        public class AzureQueueSettings
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }
        }
    }

    public class RpcSettings
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
    }

    public class ServicesSettings
    {
        public string LykkePayServiceStoreRequestMicroService { get; set; }
        public string BitcoinApiService { get; set; }
    }

    public class WalletBradcastRabbitSettings
    {
        public string ConnectionString { get; set; }
        public string ExchangeName { get; set; }
    }
}