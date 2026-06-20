using System;

namespace Stats
{
    public readonly struct StatId : IEquatable<StatId>
    {
        readonly Guid value;
        StatId(Guid value) => this.value = value;
        public Guid Value => value;
        public bool IsEmpty => value == Guid.Empty;
        public static StatId Empty => default;
        public static StatId NewId() => new StatId(Guid.NewGuid());
        public static StatId FromString(string guid) => new StatId(Guid.Parse(guid));
        public bool Equals(StatId other) => value.Equals(other.value);
        public override bool Equals(object obj) => obj is StatId other && Equals(other);
        public override int GetHashCode() => value.GetHashCode();
        public override string ToString() => value.ToString("N");
        public static bool operator ==(StatId a, StatId b) => a.Equals(b);
        public static bool operator !=(StatId a, StatId b) => !a.Equals(b);
    }
}
