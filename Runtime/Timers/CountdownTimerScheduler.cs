using System;

namespace Stats
{
    public sealed class CountdownTimerScheduler : IExpiryScheduler
    {
        public IDisposable Schedule(float seconds, Action onExpired) => new Handle(seconds, onExpired);

        sealed class Handle : IDisposable
        {
            readonly CountdownTimer timer;
            Action onExpired;

            public Handle(float seconds, Action onExpired)
            {
                this.onExpired = onExpired;
                timer = new CountdownTimer(seconds);
                timer.OnTimerStop += Fire;
                timer.Start();
            }

            void Fire()
            {
                var callback = onExpired;
                onExpired = null;
                callback?.Invoke();
            }

            public void Dispose()
            {
                onExpired = null;
                timer.OnTimerStop -= Fire;
                timer.Dispose();
            }
        }
    }
}
