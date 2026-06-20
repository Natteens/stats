using System;

namespace Stats
{
    public readonly struct ModifierHandle : IEquatable<ModifierHandle>
    {
        public StatId Stat { get; }
        internal long Id { get; }
        internal ModifierHandle(StatId stat, long id)
        {
            Stat = stat;
            Id = id;
        }
        public bool IsValid => Id != 0;
        public bool Equals(ModifierHandle other) => Id == other.Id && Stat.Equals(other.Stat);
        public override bool Equals(object obj) => obj is ModifierHandle other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
