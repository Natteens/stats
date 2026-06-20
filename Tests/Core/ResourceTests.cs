using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class ResourceTests
    {
        static StatSheet SheetWithMax(StatId maxStat, float max)
        {
            var s = new StatSheet();
            s.RegisterStat(maxStat, max);
            return s;
        }

        [Test]
        public void Resource_Reduce_Restore_Consume_ClampAtBounds()
        {
            var maxStat = StatId.NewId();
            var s = SheetWithMax(maxStat, 100f);
            var r = new RuntimeResource(s, maxStat);
            r.Reduce(30f);
            Assert.AreEqual(70f, r.Current, 1e-4f);
            r.Restore(1000f);
            Assert.AreEqual(100f, r.Current, 1e-4f);
            Assert.IsTrue(r.Consume(25f));
            Assert.AreEqual(75f, r.Current, 1e-4f);
            Assert.IsFalse(r.Consume(1000f));
            Assert.AreEqual(75f, r.Current, 1e-4f);
            r.Reduce(1000f);
            Assert.AreEqual(0f, r.Current, 1e-4f);
        }

        [Test]
        public void Resource_IsFull_True_AtMax()
        {
            var maxStat = StatId.NewId();
            var s = SheetWithMax(maxStat, 50f);
            var r = new RuntimeResource(s, maxStat);
            Assert.IsTrue(r.IsFull);
            r.Reduce(1f);
            Assert.IsFalse(r.IsFull);
        }

        [Test]
        public void Resource_MaxChange_KeepAbsolute_DoesNotHeal()
        {
            var maxStat = StatId.NewId();
            var s = SheetWithMax(maxStat, 100f);
            var r = new RuntimeResource(s, maxStat, MaxChangePolicy.KeepAbsolute);
            s.SetBaseValue(maxStat, 200f);
            Assert.AreEqual(100f, r.Current, 1e-4f);
            Assert.AreEqual(200f, r.Max, 1e-4f);
        }

        [Test]
        public void Resource_MaxChange_KeepPercent_HealsProportionally()
        {
            var maxStat = StatId.NewId();
            var s = SheetWithMax(maxStat, 100f);
            var r = new RuntimeResource(s, maxStat, MaxChangePolicy.KeepPercent);
            r.SetCurrent(50f);
            s.SetBaseValue(maxStat, 200f);
            Assert.AreEqual(100f, r.Current, 1e-4f);
        }

        [Test]
        public void Resource_MaxChange_ClampOnly_TrimsOverflow()
        {
            var maxStat = StatId.NewId();
            var s = SheetWithMax(maxStat, 100f);
            var r = new RuntimeResource(s, maxStat, MaxChangePolicy.ClampOnly);
            s.SetBaseValue(maxStat, 50f);
            Assert.AreEqual(50f, r.Current, 1e-4f);
        }
    }
}
