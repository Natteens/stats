namespace Stats
{
    public readonly struct StatSnapshot
    {
        public float Base { get; }
        public float SumFlat { get; }
        public float SumPercentAdd { get; }
        public float PercentMultProduct { get; }
        public bool HasOverride { get; }
        public float OverrideValue { get; }
        public float Final { get; }
        public bool Clamped { get; }
        public StatSnapshot(float baseValue, float sumFlat, float sumPercentAdd, float percentMultProduct,
            bool hasOverride, float overrideValue, float final, bool clamped)
        {
            Base = baseValue;
            SumFlat = sumFlat;
            SumPercentAdd = sumPercentAdd;
            PercentMultProduct = percentMultProduct;
            HasOverride = hasOverride;
            OverrideValue = overrideValue;
            Final = final;
            Clamped = clamped;
        }
    }
}
