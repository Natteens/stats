using System.Collections.Generic;

namespace Stats
{
    public static class StatDefinitionExtensions
    {
        public static StatId ToStatId(this StatDefinition def) =>
            def == null ? StatId.Empty : def.Id;

        public static void ApplyGroup(this StatSheet sheet, IReadOnlyList<StatModifierData> group, object source)
        {
            if (sheet == null || group == null) return;
            for (int i = 0; i < group.Count; i++)
            {
                var data = group[i];
                if (data.stat == null) continue;
                sheet.AddModifier(data.stat.ToStatId(), data.operation, data.value, source);
            }
        }
    }
}
