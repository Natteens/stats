using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class BreakdownTests
    {
        [Test]
        public void Snapshot_AggregatesMatchFinal()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 5f, "sword");
            s.AddPercentAdd(id, 0.5f, "rage");
            s.AddPercentMult(id, 0.25f, "shrine");
            var snap = s.GetSnapshot(id);
            Assert.AreEqual(s.GetValue(id), snap.Final, 1e-4f);
            Assert.AreEqual(5f, snap.SumFlat, 1e-4f);
            Assert.AreEqual(0.5f, snap.SumPercentAdd, 1e-4f);
            Assert.AreEqual(1.25f, snap.PercentMultProduct, 1e-4f);
            Assert.IsFalse(snap.HasOverride);
            float expected = (10f + snap.SumFlat) * (1f + snap.SumPercentAdd) * snap.PercentMultProduct;
            Assert.AreEqual(expected, snap.Final, 1e-4f);
        }

        [Test]
        public void Breakdown_ListsEachSourceUnderCorrectPhase_AndFinalMatches()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            s.AddPercentMult(id, 0.25f, "shrine");
            s.AddFlat(id, 5f, "sword");
            s.AddPercentAdd(id, 0.5f, "rage");
            var b = s.GetBreakdown(id);
            Assert.AreEqual(3, b.Entries.Count);
            Assert.AreEqual(ModifierOperation.Flat, b.Entries[0].Operation);
            Assert.AreEqual(ModifierOperation.PercentAdd, b.Entries[1].Operation);
            Assert.AreEqual(ModifierOperation.PercentMult, b.Entries[2].Operation);
            Assert.AreEqual("sword", b.Entries[0].SourceLabel);
            Assert.AreEqual(s.GetValue(id), b.Final, 1e-4f);
        }

        [Test]
        public void UnregisteredStat_Throws_TryGetIsSafe()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            Assert.Throws<StatNotRegisteredException>(() => s.GetValue(id));
            Assert.IsFalse(s.TryGetValue(id, out var value));
            Assert.AreEqual(0f, value);
        }
    }
}
