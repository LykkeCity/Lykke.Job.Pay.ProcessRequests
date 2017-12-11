using System;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    public static class DoubleExtension
    {
        public static bool BtcEqualTo(this double value1, double value2)
        {
            return Math.Abs(value1 - value2) < 0.00000001;
        }
    }
}