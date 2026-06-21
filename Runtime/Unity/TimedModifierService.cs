using System;
using System.Collections.Generic;

namespace Stats
{
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
            active.Add(new Active { sheet = sheet, handle = handle, token = token });
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
