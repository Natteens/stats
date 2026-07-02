using System;

namespace Stats
{
    public interface IExpiryHandle : IDisposable
    {
        float RemainingSeconds { get; }
    }
}
