using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class RemovalTests
    {
        [Test]
        public void RemoveModifier_ByHandle_RemovesExactlyOne()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            var h1 = s.AddFlat(id, 5f, "a");
            s.AddFlat(id, 3f, "b");
            Assert.IsTrue(s.RemoveModifier(h1));
            Assert.AreEqual(13f, s.GetValue(id), 1e-4f);
            Assert.IsFalse(s.RemoveModifier(h1));
        }

        [Test]
        public void RemoveModifiersFromSource_RemovesAllFromThatSource()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            var src = new object();
            s.AddFlat(id, 5f, src);
            s.AddPercentAdd(id, 0.5f, src);
            s.AddFlat(id, 2f, "other");
            int removed = s.RemoveModifiersFromSource(src);
            Assert.AreEqual(2, removed);
            Assert.AreEqual(12f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void TwoIdenticalItems_DifferentSources_RemoveOneLeavesOther()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            var ringA = new object();
            var ringB = new object();
            s.AddFlat(id, 5f, ringA);
            s.AddFlat(id, 5f, ringB);
            Assert.AreEqual(20f, s.GetValue(id), 1e-4f);
            s.RemoveModifiersFromSource(ringA);
            Assert.AreEqual(15f, s.GetValue(id), 1e-4f);
            Assert.IsTrue(s.HasModifierFrom(ringB));
            Assert.IsFalse(s.HasModifierFrom(ringA));
        }
    }
}
