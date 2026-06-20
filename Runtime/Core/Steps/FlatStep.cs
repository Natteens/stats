namespace Stats
{
    public sealed class FlatStep : ICalculationStep
    {
        public int Order => 100;
        public float Apply(float currentValue, in CalculationContext context)
        {
            var mods = context.Modifiers;
            float sum = 0f;
            for (int i = 0; i < mods.Count; i++)
                if (mods[i].Operation == ModifierOperation.Flat) sum += mods[i].Value;
            return currentValue + sum;
        }
    }
}
