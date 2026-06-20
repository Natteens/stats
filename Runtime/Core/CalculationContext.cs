using System.Collections.Generic;

namespace Stats
{
    public readonly struct CalculationContext
    {
        public float Base { get; }
        public IReadOnlyList<StatModifier> Modifiers { get; }
        public float Min { get; }
        public float Max { get; }
        public CalculationContext(float baseValue, IReadOnlyList<StatModifier> modifiers, float min, float max)
        {
            Base = baseValue;
            Modifiers = modifiers;
            Min = min;
            Max = max;
        }
    }
}
