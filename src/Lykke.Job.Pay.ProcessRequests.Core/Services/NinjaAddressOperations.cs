using System;
using System.Collections.Generic;

namespace Lykke.Job.Pay.ProcessRequests.Core.Services
{
    public class NinjaAddressOperations
    {
        public int Amount { get; set; }
        public int Confirmations { get; set; }
        public int Height { get; set; }
        public string BlockId { get; set; }
        public string TransactionId { get; set; }
        public List<NinjaCoins> ReceivedCoins { get; set; }
        public List<NinjaCoins> SpentCoins { get; set; }
        public DateTime FirstSeen { get; set; }
    }
}