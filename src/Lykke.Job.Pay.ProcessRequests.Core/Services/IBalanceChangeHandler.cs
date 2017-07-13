using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace Lykke.Job.Pay.ProcessRequests.Core.Services
{
    public interface IBalanceChangeHandler : IStartable, IDisposable
    {
    }
}
