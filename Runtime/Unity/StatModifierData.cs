using System;

namespace Stats
{
    [Serializable]
    public struct StatModifierData
    {
        public StatDefinition stat;
        public ModifierOperation operation;
        public float value;
    }
}
