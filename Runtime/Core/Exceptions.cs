using System;

namespace Stats
{
    public sealed class StatNotRegisteredException : Exception
    {
        public StatNotRegisteredException(StatId stat)
            : base($"Stat '{stat}' is not registered.") { }
    }
}
