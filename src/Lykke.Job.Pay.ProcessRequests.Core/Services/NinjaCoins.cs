namespace Lykke.Job.Pay.ProcessRequests.Core.Services
{
    public class NinjaCoins
    {
        public string Address { get; set; }
        public string TransactionId { get; set; }
        public int Index { get; set; }
        public int Value { get; set; }
        public string ScriptPubKey { get; set; }
    }
}