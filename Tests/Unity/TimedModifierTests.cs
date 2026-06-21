using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using Stats;

namespace Stats.Tests
{
    public sealed class TimedModifierTests
    {
        static StatDefinition Def()
        {
            var def = ScriptableObject.CreateInstance<StatDefinition>();
            typeof(StatDefinition).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(def, null);
            return def;
        }

        [Test]
        public void TimedModifier_Expires_RemovesByHandle()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var str = StatId.NewId();
            s.RegisterStat(str, 10f);
            svc.AddTimedModifier(s, str, ModifierOperation.PercentAdd, 0.5f, "buff", 8f);
            Assert.AreEqual(15f, s.GetValue(str), 1e-4f);
            clock.Advance(8f);
            Assert.AreEqual(10f, s.GetValue(str), 1e-4f);
        }

        [Test]
        public void TimedModifier_BeforeExpiry_StillApplied()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var str = StatId.NewId();
            s.RegisterStat(str, 10f);
            svc.AddTimedModifier(s, str, ModifierOperation.PercentAdd, 0.5f, "buff", 8f);
            clock.Advance(4f);
            Assert.AreEqual(15f, s.GetValue(str), 1e-4f);
        }

        [Test]
        public void CancelSchedules_DisposesTimers_DoesNotRemoveModifiers()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var str = StatId.NewId();
            s.RegisterStat(str, 10f);
            svc.AddTimedModifier(s, str, ModifierOperation.PercentAdd, 0.5f, "buff", 8f);
            svc.CancelSchedules();
            Assert.AreEqual(15f, s.GetValue(str), 1e-4f);
            clock.Advance(100f);
            Assert.AreEqual(15f, s.GetValue(str), 1e-4f);
        }

        [Test]
        public void ClearTimedModifiers_DisposesTimers_AndRemovesModifiers()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var s = new StatSheet();
            var str = StatId.NewId();
            s.RegisterStat(str, 10f);
            svc.AddTimedModifier(s, str, ModifierOperation.PercentAdd, 0.5f, "buff", 8f);
            svc.ClearTimedModifiers();
            Assert.AreEqual(10f, s.GetValue(str), 1e-4f);
            clock.Advance(100f);
            Assert.AreEqual(10f, s.GetValue(str), 1e-4f);
        }

        [Test]
        public void ApplyTimedGroup_AppliesEveryModifier_AndExpiresAll()
        {
            var clock = new FakeScheduler();
            var svc = new TimedModifierService(clock);
            var def = Def();
            var s = new StatSheet();
            s.RegisterStat(def.ToStatId(), 10f);
            var group = new[]
            {
                new StatModifierData { stat = def, operation = ModifierOperation.Flat, value = 5f },
                new StatModifierData { stat = def, operation = ModifierOperation.PercentAdd, value = 0.5f }
            };
            svc.ApplyTimedGroup(s, group, "rage", 8f);
            Assert.AreEqual(22.5f, s.GetValue(def.ToStatId()), 1e-4f);
            clock.Advance(8f);
            Assert.AreEqual(10f, s.GetValue(def.ToStatId()), 1e-4f);
            Object.DestroyImmediate(def);
        }
    }
}
