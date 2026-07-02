using System.Collections.Generic;

namespace Stats
{
    public static class StatSheetSaveUtility
    {
        public static StatSheetSaveData Capture(StatSheetBehaviour behaviour)
        {
            var data = new StatSheetSaveData { version = StatSaveInterop.Version };
            if (behaviour == null || behaviour.Sheet == null) return data;
            var sheet = behaviour.Sheet;

            foreach (var stat in sheet.GetRegisteredStats())
                data.bases.Add(new StatBaseSaveData { statId = stat.ToString(), baseValue = sheet.GetBaseValue(stat), statKey = KeyOf(behaviour, stat) });

            int order = 0;
            var timedHandles = new HashSet<ModifierHandle>();
            if (behaviour.TimedModifiers != null)
            {
                foreach (var t in behaviour.TimedModifiers.GetActiveTimedModifiers())
                {
                    timedHandles.Add(t.Handle);
                    if (t.SourceId == null) { order++; continue; }
                    data.modifiers.Add(new StatModifierSaveData
                    {
                        statId = t.Stat.ToString(),
                        operation = (int)t.Operation,
                        value = t.Value,
                        sourceId = t.SourceId,
                        timed = true,
                        remainingSeconds = t.RemainingSeconds,
                        order = order++,
                        statKey = KeyOf(behaviour, t.Stat)
                    });
                }
            }

            foreach (var entry in sheet.GetModifierEntries())
            {
                if (timedHandles.Contains(entry.Handle)) continue;
                string sourceId = StatSourceId.Of(entry.Source);
                if (sourceId == null) { order++; continue; }
                data.modifiers.Add(new StatModifierSaveData
                {
                    statId = entry.Handle.Stat.ToString(),
                    operation = (int)entry.Operation,
                    value = entry.Value,
                    sourceId = sourceId,
                    timed = false,
                    remainingSeconds = 0f,
                    order = order++,
                    statKey = KeyOf(behaviour, entry.Handle.Stat)
                });
            }

            foreach (var pair in behaviour.Resources)
                data.resources.Add(ResourceSaveInterop.Capture(pair.Key, pair.Value));

            return data;
        }

        public static void Restore(StatSheetBehaviour behaviour, StatSheetSaveData data)
        {
            if (behaviour == null || behaviour.Sheet == null || data == null) return;
            var sheet = behaviour.Sheet;

            behaviour.TimedModifiers?.CancelSchedules();
            StatSaveInterop.Restore(sheet, data);

            if (behaviour.TimedModifiers != null && data.modifiers != null)
            {
                foreach (var m in data.modifiers)
                {
                    if (!m.timed || m.remainingSeconds <= 0f) continue;
                    var id = StatId.FromString(m.statId);
                    if (!sheet.IsRegistered(id)) continue;
                    behaviour.TimedModifiers.AddTimedModifier(sheet, id, (ModifierOperation)m.operation, m.value,
                        new RestoredSource(m.sourceId), m.remainingSeconds);
                }
            }

            if (data.resources != null)
            {
                foreach (var r in data.resources)
                    if (behaviour.TryGetResource(r.key, out var resource)) ResourceSaveInterop.Restore(resource, r);
            }
        }
        static string KeyOf(StatSheetBehaviour behaviour, StatId statId) =>
            behaviour.TryGetKey(statId, out var key) ? key : null;
    }

    public static class StatSheetBehaviourSaveExtensions
    {
        public static StatSheetSaveData CaptureSaveData(this StatSheetBehaviour behaviour) => StatSheetSaveUtility.Capture(behaviour);
        public static void RestoreSaveData(this StatSheetBehaviour behaviour, StatSheetSaveData data) => StatSheetSaveUtility.Restore(behaviour, data);
    }
}
