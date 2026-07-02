using System.Collections.Generic;

namespace Stats.Editor
{
    public static class PresetValidation
    {
        public static List<int> FindNullEntries(IReadOnlyList<StatSheetPreset.Entry> entries)
        {
            var result = new List<int>();
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].stat == null) result.Add(i);
            return result;
        }

        public static List<StatDefinition> FindDuplicateStats(IReadOnlyList<StatSheetPreset.Entry> entries)
        {
            var counts = new Dictionary<StatId, int>();
            var first = new Dictionary<StatId, StatDefinition>();
            for (int i = 0; i < entries.Count; i++)
            {
                var stat = entries[i].stat;
                if (stat == null) continue;
                var id = stat.ToStatId();
                if (id.IsEmpty) continue;
                counts.TryGetValue(id, out int c);
                counts[id] = c + 1;
                if (!first.ContainsKey(id)) first[id] = stat;
            }
            var result = new List<StatDefinition>();
            foreach (var pair in counts)
                if (pair.Value > 1) result.Add(first[pair.Key]);
            return result;
        }

        public static List<StatDefinition> FindDuplicateKeys(IReadOnlyList<StatSheetPreset.Entry> entries)
        {
            var counts = new Dictionary<string, int>(System.StringComparer.Ordinal);
            var first = new Dictionary<string, StatDefinition>(System.StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                var stat = entries[i].stat;
                if (stat == null || string.IsNullOrEmpty(stat.Key)) continue;
                counts.TryGetValue(stat.Key, out int c);
                counts[stat.Key] = c + 1;
                if (!first.ContainsKey(stat.Key)) first[stat.Key] = stat;
            }
            var result = new List<StatDefinition>();
            foreach (var pair in counts)
                if (pair.Value > 1) result.Add(first[pair.Key]);
            return result;
        }

        public static bool HasMinGreaterThanMax(in StatSheetPreset.Entry entry) =>
            entry.overrideMin && entry.overrideMax && entry.min > entry.max;
    }
}
