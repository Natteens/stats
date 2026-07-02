using System.Collections.Generic;

namespace Stats
{
    public static class StatSaveInterop
    {
        public const int Version = 1;

        public static StatSheetSaveData Capture(StatSheet sheet)
        {
            var data = new StatSheetSaveData { version = Version };
            if (sheet == null) return data;

            foreach (var stat in sheet.GetRegisteredStats())
                data.bases.Add(new StatBaseSaveData { statId = stat.ToString(), baseValue = sheet.GetBaseValue(stat) });

            int order = 0;
            foreach (var entry in sheet.GetModifierEntries())
            {
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
                    order = order++
                });
            }
            return data;
        }

        public static void Restore(StatSheet sheet, StatSheetSaveData data)
        {
            if (sheet == null || data == null) return;

            foreach (var stat in sheet.GetRegisteredStats())
                sheet.ClearModifiers(stat);

            if (data.bases != null)
            {
                foreach (var b in data.bases)
                {
                    var id = StatId.FromString(b.statId);
                    if (sheet.IsRegistered(id)) sheet.SetBaseValue(id, b.baseValue);
                }
            }

            if (data.modifiers == null) return;
            var ordered = new List<StatModifierSaveData>(data.modifiers);
            ordered.Sort((x, y) => x.order.CompareTo(y.order));
            var sources = new Dictionary<string, RestoredSource>();
            foreach (var m in ordered)
            {
                if (m.timed) continue;
                var id = StatId.FromString(m.statId);
                if (!sheet.IsRegistered(id)) continue;
                sheet.AddModifier(id, (ModifierOperation)m.operation, m.value, SourceFor(sources, m.sourceId));
            }
        }

        static RestoredSource SourceFor(Dictionary<string, RestoredSource> cache, string sourceId)
        {
            string key = sourceId ?? string.Empty;
            if (!cache.TryGetValue(key, out var source))
            {
                source = new RestoredSource(key);
                cache[key] = source;
            }
            return source;
        }
    }
}
