namespace Stats
{
    public sealed class PercentAddStep : ICalculationStep
    {
        public int Order => 200;
        public float Apply(float currentValue, in CalculationContext context)
        {
            var mods = context.Modifiers;
            float sum = 0f;
            for (int i = 0; i < mods.Count; i++)
                if (mods[i].Operation == ModifierOperation.PercentAdd) sum += mods[i].Value;
            return currentValue * (1f + sum);
        }
    }
}
