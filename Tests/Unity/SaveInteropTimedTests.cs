using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class SaveInteropTimedTests
    {
        [Test]
        public void Capture_TimedModifier_WithRemaining()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var attack = StatId.NewId();
            s.RegisterStat(attack, 10f);
            svc.AddTimedModifier(s, attack, ModifierOperation.PercentAdd, 0.5f, "rage", 8f);
            clock.Advance(3f);

            var snapshots = svc.GetActiveTimedModifiers();
            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual(5f, snapshots[0].RemainingSeconds, 1e-4f);
            Assert.AreEqual("rage", snapshots[0].SourceId);
            Assert.AreEqual(ModifierOperation.PercentAdd, snapshots[0].Operation);
        }

        [Test]
        public void Restore_TimedModifier_AppliesAndExpires()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var attack = StatId.NewId();
            s.RegisterStat(attack, 10f);
            svc.AddTimedModifier(s, attack, ModifierOperation.PercentAdd, 0.5f, new RestoredSource("rage"), 5f);
            Assert.AreEqual(15f, s.GetValue(attack), 1e-4f);
            clock.Advance(5f);
            Assert.AreEqual(10f, s.GetValue(attack), 1e-4f);
        }

        [Test]
        public void Consumable_Source_Restore_WhenOriginalObjectGone()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var attack = StatId.NewId();
            s.RegisterStat(attack, 10f);
            var consumable = new RestoredSource("potion_1");
            svc.AddTimedModifier(s, attack, ModifierOperation.Flat, 4f, consumable, 10f);
            clock.Advance(6f);
            var snap = svc.GetActiveTimedModifiers()[0];
            string savedSourceId = snap.SourceId;
            float savedRemaining = snap.RemainingSeconds;

            var clock2 = new FakeScheduler();
            var svc2 = new TimedModifierService(clock2);
            var s2 = new StatSheet();
            s2.RegisterStat(attack, 10f);
            svc2.AddTimedModifier(s2, attack, ModifierOperation.Flat, 4f, new RestoredSource(savedSourceId), savedRemaining);
            Assert.AreEqual(14f, s2.GetValue(attack), 1e-4f);
            clock2.Advance(savedRemaining);
            Assert.AreEqual(10f, s2.GetValue(attack), 1e-4f);
        }

        [Test]
        public void CancelSchedules_DisposesTimers_DoesNotRemoveModifiers()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var attack = StatId.NewId();
            s.RegisterStat(attack, 10f);
            svc.AddTimedModifier(s, attack, ModifierOperation.PercentAdd, 0.5f, "rage", 8f);
            svc.CancelSchedules();
            Assert.AreEqual(15f, s.GetValue(attack), 1e-4f);
            clock.Advance(100f);
            Assert.AreEqual(15f, s.GetValue(attack), 1e-4f);
            Assert.AreEqual(0, svc.GetActiveTimedModifiers().Count);
        }

        [Test]
        public void ClearTimedModifiers_DisposesTimers_AndRemovesModifiers()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var attack = StatId.NewId();
            s.RegisterStat(attack, 10f);
            svc.AddTimedModifier(s, attack, ModifierOperation.PercentAdd, 0.5f, "rage", 8f);
            svc.ClearTimedModifiers();
            Assert.AreEqual(10f, s.GetValue(attack), 1e-4f);
            Assert.AreEqual(0, svc.GetActiveTimedModifiers().Count);
        }
    }
}
