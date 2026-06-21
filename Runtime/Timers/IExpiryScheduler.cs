using System;

namespace Stats
{
    public interface IExpiryScheduler
    {
        IDisposable Schedule(float seconds, Action onExpired);
    }
}
