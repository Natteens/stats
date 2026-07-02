namespace Stats
{
    public sealed class RestoredSource : IStatModifierSource
    {
        public string SourceId { get; }
        public RestoredSource(string sourceId) => SourceId = sourceId ?? string.Empty;
        public override string ToString() => SourceId;
    }
}
