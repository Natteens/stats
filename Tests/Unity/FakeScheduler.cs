using System;
using System.Collections.Generic;
using Stats;

namespace Stats.Tests
{
    public sealed class FakeScheduler : IExpiryScheduler
    {
        sealed class Entry
        {
            public float remaining;
            public Action onExpired;
            public bool dead;
        }

        readonly List<Entry> entries = new List<Entry>();

        public IDisposable Schedule(float seconds, Action onExpired)
        {
            var entry = new Entry { remaining = seconds, onExpired = onExpired };
            entries.Add(entry);
            return new Token(entry);
        }

        public void Advance(float seconds)
        {
            var snapshot = entries.ToArray();
            foreach (var entry in snapshot)
            {
                if (entry.dead) continue;
                entry.remaining -= seconds;
                if (entry.remaining <= 0f)
                {
                    entry.dead = true;
                    entry.onExpired?.Invoke();
                }
            }
        }

        sealed class Token : IDisposable
        {
            readonly Entry entry;
            public Token(Entry entry) => this.entry = entry;
            public void Dispose()
            {
                entry.dead = true;
                entry.onExpired = null;
            }
        }
    }
}
