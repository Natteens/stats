namespace Stats
{
    public sealed class ClampStep : ICalculationStep
    {
        public int Order => 1000;
        public float Apply(float currentValue, in CalculationContext context)
        {
            if (currentValue < context.Min) return context.Min;
            if (currentValue > context.Max) return context.Max;
            return currentValue;
        }
    }
}
