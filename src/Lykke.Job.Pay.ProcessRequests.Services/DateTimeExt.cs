using System;
using System.Globalization;

namespace Lykke.Job.Pay.ProcessRequests.Services
{
    public static class DateTimeExt
    {
        public static string RepoDateStr(this DateTime date)
        {
            return date.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
        }


    }
}