using System.Collections.Generic;
using Lykke.AzureRepositories;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    public class MerchantOrderAddress
    {
        public string Address { get; }
        public List<MerchantOrderRequest> Orders { get; }

        public MerchantOrderAddress(string address, List<MerchantOrderRequest> orders)
        {
            Address = address;
            Orders = orders;
        }
    }
}