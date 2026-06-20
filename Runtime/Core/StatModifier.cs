using System;

namespace Stats
{
    public readonly struct StatModifier
    {
        public ModifierOperation Operation { get; }
        public float Value { get; }
        public object Source { get; }
        public StatModifier(ModifierOperation operation, float value, object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Operation = operation;
            Value = value;
            Source = source;
        }
    }
}
