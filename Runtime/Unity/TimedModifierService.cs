using System;
using System.Collections.Generic;

namespace Stats
{
    public readonly struct TimedModifierSnapshot
    {
        public ModifierHandle Handle { get; }
        public StatId Stat { get; }
        public ModifierOperation Operation { get; }
        public float Value { get; }
        public string SourceId { get; }
        public float RemainingSeconds { get; }
        public TimedModifierSnapshot(ModifierHandle handle, StatId stat, ModifierOperation operation, float value, string sourceId, float remainingSeconds)
        {
            Handle = handle;
            Stat = stat;
            Operation = operation;
            Value = value;
            SourceId = sourceId;
            RemainingSeconds = remainingSeconds;
        }
    }

    public sealed class TimedModifierService
    {
        readonly IExpiryScheduler scheduler;
        readonly List<Active> active = new List<Active>();

        public TimedModifierService(IExpiryScheduler scheduler)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        struct Active
        {
            public StatSheet sheet;
            public ModifierHandle handle;
            public IDisposable token;
            public ModifierOperation operation;
            public float value;
            public object source;
        }

        public ModifierHandle AddTimedModifier(StatSheet sheet, StatId stat, ModifierOperation op, float value, object source, float durationSeconds)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            // Non-positive duration: do not apply, so there is no transient effect to revert.
            if (durationSeconds <= 0f) return default;

            var handle = sheet.AddModifier(stat, op, value, source);
            var token = scheduler.Schedule(durationSeconds, () =>
            {
                sheet.RemoveModifier(handle);
                RemoveActive(handle);
            });
            active.Add(new Active { sheet = sheet, handle = handle, token = token, operation = op, value = value, source = source });
            return handle;
        }

        public void ApplyTimedGroup(StatSheet sheet, IReadOnlyList<StatModifierData> group, object source, float durationSeconds)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (group == null) return;
            for (int i = 0; i < group.Count; i++)
            {
                var data = group[i];
                if (data.stat == null) continue;
                AddTimedModifier(sheet, data.stat.ToStatId(), data.operation, data.value, source, durationSeconds);
            }
        }

        public IReadOnlyList<TimedModifierSnapshot> GetActiveTimedModifiers()
        {
            var list = new List<TimedModifierSnapshot>();
            for (int i = 0; i < active.Count; i++)
            {
                var a = active[i];
                float remaining = a.token is IExpiryHandle handle ? handle.RemainingSeconds : 0f;
                if (remaining <= 0f) continue;
                list.Add(new TimedModifierSnapshot(a.handle, a.handle.Stat, a.operation, a.value, StatSourceId.Of(a.source), remaining));
            }
            return list;
        }

        public void CancelSchedules()
        {
            for (int i = 0; i < active.Count; i++) active[i].token?.Dispose();
            active.Clear();
        }

        public void ClearTimedModifiers()
        {
            for (int i = 0; i < active.Count; i++)
            {
                active[i].token?.Dispose();
                active[i].sheet.RemoveModifier(active[i].handle);
            }
            active.Clear();
        }

        void RemoveActive(ModifierHandle handle)
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].handle.Equals(handle)) { active.RemoveAt(i); break; }
        }
    }
}
