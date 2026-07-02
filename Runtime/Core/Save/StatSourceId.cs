namespace Stats
{
    public static class StatSourceId
    {
        public static string Of(object source)
        {
            if (source is IStatModifierSource typed) return typed.SourceId;
            if (source is string text) return text;
            return null;
        }
    }
}
