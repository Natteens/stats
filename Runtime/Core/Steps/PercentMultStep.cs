namespace Stats
{
    public sealed class PercentMultStep : ICalculationStep
    {
        public int Order => 300;
        public float Apply(float currentValue, in CalculationContext context)
        {
            var mods = context.Modifiers;
            float product = 1f;
            for (int i = 0; i < mods.Count; i++)
                if (mods[i].Operation == ModifierOperation.PercentMult) product *= 1f + mods[i].Value;
            return currentValue * product;
        }
    }
}
