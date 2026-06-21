namespace Stats.Editor
{
    public static class PercentConversion
    {
        public static bool IsPercent(ModifierOperation op) =>
            op == ModifierOperation.PercentAdd || op == ModifierOperation.PercentMult;

        public static float ToDisplay(float stored, ModifierOperation op) =>
            IsPercent(op) ? stored * 100f : stored;

        public static float ToStored(float display, ModifierOperation op) =>
            IsPercent(op) ? display / 100f : display;
    }
}
