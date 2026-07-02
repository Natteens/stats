using System;
using System.Collections.Generic;

namespace Stats
{
    public sealed class StatSheet
    {
        readonly Dictionary<StatId, RuntimeStat> stats = new Dictionary<StatId, RuntimeStat>();
        readonly IStatCalculator calculator;
        long handleCounter;

        public event Action<StatId, float> ValueChanged;

        public StatSheet(IStatCalculator calculator = null)
        {
            this.calculator = calculator ?? PipelineStatCalculator.CreateDefault();
        }

        public void RegisterStat(StatId stat, float baseValue,
            float min = float.NegativeInfinity, float max = float.PositiveInfinity)
        {
            var runtime = new RuntimeStat(baseValue, min, max, calculator);
            runtime.ValueChanged += value => ValueChanged?.Invoke(stat, value);
            stats[stat] = runtime;
        }

        public bool IsRegistered(StatId stat) => stats.ContainsKey(stat);

        public float GetValue(StatId stat) => Require(stat).Value;

        public bool TryGetValue(StatId stat, out float value)
        {
            if (stats.TryGetValue(stat, out var runtime))
            {
                value = runtime.Value;
                return true;
            }
            value = 0f;
            return false;
        }

        public StatSnapshot GetSnapshot(StatId stat) => Require(stat).GetSnapshot();
        public StatBreakdown GetBreakdown(StatId stat) => Require(stat).GetBreakdown(stat);
        public float GetBaseValue(StatId stat) => Require(stat).BaseValue;
        public void SetBaseValue(StatId stat, float value) => Require(stat).SetBaseValue(value);

        public ModifierHandle AddModifier(StatId stat, ModifierOperation op, float value, object source)
        {
            var runtime = Require(stat);
            var modifier = new StatModifier(op, value, source);
            long id = ++handleCounter;
            runtime.AddModifier(id, modifier);
            return new ModifierHandle(stat, id);
        }

        public ModifierHandle AddFlat(StatId stat, float value, object source) =>
            AddModifier(stat, ModifierOperation.Flat, value, source);

        public ModifierHandle AddPercentAdd(StatId stat, float fraction, object source) =>
            AddModifier(stat, ModifierOperation.PercentAdd, fraction, source);

        public ModifierHandle AddPercentMult(StatId stat, float fraction, object source) =>
            AddModifier(stat, ModifierOperation.PercentMult, fraction, source);

        public ModifierHandle AddOverride(StatId stat, float value, object source) =>
            AddModifier(stat, ModifierOperation.Override, value, source);

        public bool RemoveModifier(ModifierHandle handle)
        {
            if (!handle.IsValid) return false;
            return stats.TryGetValue(handle.Stat, out var runtime) && runtime.RemoveModifier(handle.Id);
        }

        public int RemoveModifiersFromSource(object source)
        {
            if (source == null) return 0;
            int removed = 0;
            foreach (var runtime in stats.Values)
                removed += runtime.RemoveModifiersFromSource(source);
            return removed;
        }

        public bool HasModifierFrom(object source)
        {
            if (source == null) return false;
            foreach (var runtime in stats.Values)
                if (runtime.HasModifierFrom(source)) return true;
            return false;
        }

        public void ClearModifiers(StatId stat) => Require(stat).ClearModifiers();

        public IReadOnlyList<StatModifier> GetModifiers(StatId stat) => Require(stat).GetModifiers();

        public IReadOnlyList<StatId> GetRegisteredStats()
        {
            var list = new List<StatId>(stats.Count);
            foreach (var key in stats.Keys) list.Add(key);
            return list;
        }

        public IReadOnlyList<ModifierEntry> GetModifierEntries()
        {
            var list = new List<ModifierEntry>();
            foreach (var pair in stats) pair.Value.CollectEntries(pair.Key, list);
            return list;
        }

        RuntimeStat Require(StatId stat)
        {
            if (!stats.TryGetValue(stat, out var runtime)) throw new StatNotRegisteredException(stat);
            return runtime;
        }
    }
}
