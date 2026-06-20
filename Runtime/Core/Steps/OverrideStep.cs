namespace Stats
{
    public sealed class OverrideStep : ICalculationStep
    {
        public int Order => 400;
        public float Apply(float currentValue, in CalculationContext context)
        {
            var mods = context.Modifiers;
            bool has = false;
            float value = 0f;
            for (int i = 0; i < mods.Count; i++)
                if (mods[i].Operation == ModifierOperation.Override)
                {
                    has = true;
                    value = mods[i].Value;
                }
            return has ? value : currentValue;
        }
    }
}
