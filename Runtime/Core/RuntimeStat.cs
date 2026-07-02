using System;
using System.Collections.Generic;

namespace Stats
{
    public sealed class RuntimeStat
    {
        readonly List<StatModifier> modifiers = new List<StatModifier>();
        readonly List<long> ids = new List<long>();
        readonly IStatCalculator calculator;
        float baseValue;
        readonly float min;
        readonly float max;
        float cached;
        bool dirty = true;
        float lastNotified = float.NaN;

        public event Action<float> ValueChanged;

        public RuntimeStat(float baseValue, float min, float max, IStatCalculator calculator)
        {
            this.baseValue = baseValue;
            this.min = min;
            this.max = max;
            this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        public float BaseValue => baseValue;
        public float Min => min;
        public float Max => max;

        public float Value
        {
            get
            {
                if (dirty) Recalculate();
                return cached;
            }
        }

        public void SetBaseValue(float value)
        {
            if (value == baseValue) return;
            baseValue = value;
            MarkDirtyAndNotify();
        }

        internal void AddModifier(long id, in StatModifier modifier)
        {
            ids.Add(id);
            modifiers.Add(modifier);
            MarkDirtyAndNotify();
        }

        internal bool RemoveModifier(long id)
        {
            int index = ids.IndexOf(id);
            if (index < 0) return false;
            ids.RemoveAt(index);
            modifiers.RemoveAt(index);
            MarkDirtyAndNotify();
            return true;
        }

        internal int RemoveModifiersFromSource(object source)
        {
            int removed = 0;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(modifiers[i].Source, source))
                {
                    modifiers.RemoveAt(i);
                    ids.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0) MarkDirtyAndNotify();
            return removed;
        }

        internal bool HasModifierFrom(object source)
        {
            for (int i = 0; i < modifiers.Count; i++)
                if (ReferenceEquals(modifiers[i].Source, source)) return true;
            return false;
        }

        internal void ClearModifiers()
        {
            if (modifiers.Count == 0) return;
            modifiers.Clear();
            ids.Clear();
            MarkDirtyAndNotify();
        }

        internal void CollectEntries(StatId stat, List<ModifierEntry> output)
        {
            for (int i = 0; i < modifiers.Count; i++)
                output.Add(new ModifierEntry(new ModifierHandle(stat, ids[i]), modifiers[i].Operation, modifiers[i].Value, modifiers[i].Source));
        }

        public IReadOnlyList<StatModifier> GetModifiers() => modifiers.ToArray();

        public StatSnapshot GetSnapshot()
        {
            float sumFlat = 0f, sumPercentAdd = 0f, multProduct = 1f, overrideValue = 0f;
            bool hasOverride = false;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                switch (m.Operation)
                {
                    case ModifierOperation.Flat: sumFlat += m.Value; break;
                    case ModifierOperation.PercentAdd: sumPercentAdd += m.Value; break;
                    case ModifierOperation.PercentMult: multProduct *= 1f + m.Value; break;
                    case ModifierOperation.Override: hasOverride = true; overrideValue = m.Value; break;
                }
            }
            float preClamp = hasOverride
                ? overrideValue
                : (baseValue + sumFlat) * (1f + sumPercentAdd) * multProduct;
            float final = Value;
            return new StatSnapshot(baseValue, sumFlat, sumPercentAdd, multProduct,
                hasOverride, overrideValue, final, final != preClamp);
        }

        public StatBreakdown GetBreakdown(StatId stat)
        {
            var entries = new List<BreakdownEntry>(modifiers.Count);
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                entries.Add(new BreakdownEntry(m.Operation, m.Value, m.Source?.ToString()));
            }
            entries.Sort((a, b) => ((int)a.Operation).CompareTo((int)b.Operation));
            return new StatBreakdown(stat, baseValue, Value, entries);
        }

        void MarkDirtyAndNotify()
        {
            dirty = true;
            float value = Value;
            if (value == lastNotified) return;
            lastNotified = value;
            ValueChanged?.Invoke(value);
        }

        void Recalculate()
        {
            var context = new CalculationContext(baseValue, modifiers, min, max);
            cached = calculator.Calculate(context);
            dirty = false;
        }
    }
}
