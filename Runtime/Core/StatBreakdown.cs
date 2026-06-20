using System.Collections.Generic;

namespace Stats
{
    public readonly struct BreakdownEntry
    {
        public ModifierOperation Operation { get; }
        public float Value { get; }
        public string SourceLabel { get; }
        public BreakdownEntry(ModifierOperation operation, float value, string sourceLabel)
        {
            Operation = operation;
            Value = value;
            SourceLabel = sourceLabel;
        }
    }

    public sealed class StatBreakdown
    {
        public StatId Stat { get; }
        public float Base { get; }
        public float Final { get; }
        public IReadOnlyList<BreakdownEntry> Entries { get; }
        public StatBreakdown(StatId stat, float baseValue, float final, IReadOnlyList<BreakdownEntry> entries)
        {
            Stat = stat;
            Base = baseValue;
            Final = final;
            Entries = entries;
        }
    }
}
