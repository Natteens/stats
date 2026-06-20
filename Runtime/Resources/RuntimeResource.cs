using System;

namespace Stats
{
    public sealed class RuntimeResource
    {
        readonly StatSheet sheet;
        readonly StatId maxStat;
        readonly MaxChangePolicy policy;
        float current;
        float lastMax;

        public event Action<float, float> Changed;
        public event Action Emptied;

        public RuntimeResource(StatSheet sheet, StatId maxStat,
            MaxChangePolicy policy = MaxChangePolicy.KeepAbsolute, bool startFull = true)
        {
            this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
            this.maxStat = maxStat;
            this.policy = policy;
            lastMax = Max;
            current = startFull ? lastMax : 0f;
            sheet.ValueChanged += OnStatChanged;
        }

        public float Current => current;
        public float Max => sheet.GetValue(maxStat);
        public bool IsFull => current >= Max;

        public float Normalized
        {
            get
            {
                float max = Max;
                return max > 0f ? current / max : 0f;
            }
        }

        public void Reduce(float amount)
        {
            if (amount <= 0f) return;
            SetCurrentInternal(current - amount);
        }

        public void Restore(float amount)
        {
            if (amount <= 0f) return;
            SetCurrentInternal(current + amount);
        }

        public bool Consume(float amount)
        {
            if (amount < 0f) return false;
            if (current < amount) return false;
            SetCurrentInternal(current - amount);
            return true;
        }

        public void SetCurrent(float value) => SetCurrentInternal(value);

        void SetCurrentInternal(float value)
        {
            float max = Max;
            float clamped = Clamp(value, 0f, max);
            if (clamped == current) return;
            bool wasEmpty = current <= 0f;
            current = clamped;
            Changed?.Invoke(current, max);
            if (current <= 0f && !wasEmpty) Emptied?.Invoke();
        }

        void OnStatChanged(StatId stat, float value)
        {
            if (stat != maxStat) return;
            float newMax = value;
            float target = policy == MaxChangePolicy.KeepPercent
                ? (lastMax > 0f ? current / lastMax : 0f) * newMax
                : current;
            lastMax = newMax;
            bool wasEmpty = current <= 0f;
            current = Clamp(target, 0f, newMax);
            Changed?.Invoke(current, newMax);
            if (current <= 0f && !wasEmpty) Emptied?.Invoke();
        }

        static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
