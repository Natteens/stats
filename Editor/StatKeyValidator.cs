using System.Collections.Generic;

namespace Stats.Editor
{
    public static class StatKeyValidator
    {
        public static List<StatDefinition> FindEmptyKeys(IReadOnlyList<StatDefinition> defs)
        {
            var result = new List<StatDefinition>();
            for (int i = 0; i < defs.Count; i++)
                if (defs[i] != null && string.IsNullOrEmpty(defs[i].Key)) result.Add(defs[i]);
            return result;
        }

        public static List<StatDefinition> FindInvalidKeys(IReadOnlyList<StatDefinition> defs)
        {
            var result = new List<StatDefinition>();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null || string.IsNullOrEmpty(def.Key)) continue;
                if (!StatKey.IsValid(def.Key)) result.Add(def);
            }
            return result;
        }

        public static List<StatDefinition> FindDuplicateKeys(IReadOnlyList<StatDefinition> defs)
        {
            var counts = new Dictionary<string, int>(System.StringComparer.Ordinal);
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null || string.IsNullOrEmpty(def.Key)) continue;
                counts.TryGetValue(def.Key, out int c);
                counts[def.Key] = c + 1;
            }
            var result = new List<StatDefinition>();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null || string.IsNullOrEmpty(def.Key)) continue;
                if (counts[def.Key] > 1) result.Add(def);
            }
            return result;
        }

        public static bool IsDuplicateKey(StatDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Key)) return false;
            int count = 0;
            foreach (var other in StatIdValidator.LoadAll())
                if (other != null && string.Equals(other.Key, def.Key, System.StringComparison.Ordinal)) count++;
            return count > 1;
        }
    }
}
