namespace Stats
{
    public readonly struct ModifierEntry
    {
        public ModifierHandle Handle { get; }
        public ModifierOperation Operation { get; }
        public float Value { get; }
        public object Source { get; }
        public ModifierEntry(ModifierHandle handle, ModifierOperation operation, float value, object source)
        {
            Handle = handle;
            Operation = operation;
            Value = value;
            Source = source;
        }
    }
}
