using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class SaveInteropTests
    {
        [Test]
        public void Capture_Restore_BaseValues()
        {
            var s = new StatSheet();
            var a = StatId.NewId();
            var b = StatId.NewId();
            s.RegisterStat(a, 10f);
            s.RegisterStat(b, 5f);
            s.SetBaseValue(a, 12f);

            var data = StatSaveInterop.Capture(s);
            var restored = new StatSheet();
            restored.RegisterStat(a, 0f);
            restored.RegisterStat(b, 0f);
            StatSaveInterop.Restore(restored, data);

            Assert.AreEqual(12f, restored.GetValue(a), 1e-4f);
            Assert.AreEqual(5f, restored.GetValue(b), 1e-4f);
        }

        [Test]
        public void Capture_Restore_AllModifierKinds()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 5f, "sword");
            s.AddPercentAdd(id, 0.5f, "rage");
            s.AddPercentMult(id, 0.25f, "shrine");
            float expected = s.GetValue(id);

            var data = StatSaveInterop.Capture(s);
            var restored = new StatSheet();
            restored.RegisterStat(id, 10f);
            StatSaveInterop.Restore(restored, data);

            Assert.AreEqual(expected, restored.GetValue(id), 1e-4f);
        }

        [Test]
        public void Restore_PreservesOverrideOrder()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            s.AddOverride(id, 5f, "a");
            s.AddOverride(id, 8f, "b");
            Assert.AreEqual(8f, s.GetValue(id), 1e-4f);

            var data = StatSaveInterop.Capture(s);
            var restored = new StatSheet();
            restored.RegisterStat(id, 10f);
            StatSaveInterop.Restore(restored, data);
            Assert.AreEqual(8f, restored.GetValue(id), 1e-4f);
        }

        [Test]
        public void Restore_DoesNotDuplicateExistingModifiers()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 5f, "sword");

            var data = StatSaveInterop.Capture(s);
            StatSaveInterop.Restore(s, data);
            Assert.AreEqual(15f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void Restore_SkipsUnknownStat_Deterministically()
        {
            var known = StatId.NewId();
            var unknown = StatId.NewId();
            var data = new StatSheetSaveData { version = StatSaveInterop.Version };
            data.bases.Add(new StatBaseSaveData { statId = unknown.ToString(), baseValue = 99f });
            data.modifiers.Add(new StatModifierSaveData
            {
                statId = unknown.ToString(), operation = (int)ModifierOperation.Flat, value = 5f, sourceId = "x", timed = false, order = 0
            });

            var s = new StatSheet();
            s.RegisterStat(known, 10f);
            Assert.DoesNotThrow(() => StatSaveInterop.Restore(s, data));
            Assert.AreEqual(10f, s.GetValue(known), 1e-4f);
            Assert.IsFalse(s.IsRegistered(unknown));
        }

        [Test]
        public void NonSerializableSource_IsNotCaptured()
        {
            var s = new StatSheet();
            var id = StatId.NewId();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 5f, new object());
            s.AddFlat(id, 3f, "belt");

            var data = StatSaveInterop.Capture(s);
            Assert.AreEqual(1, data.modifiers.Count);
            Assert.AreEqual("belt", data.modifiers[0].sourceId);

            var restored = new StatSheet();
            restored.RegisterStat(id, 10f);
            StatSaveInterop.Restore(restored, data);
            Assert.AreEqual(13f, restored.GetValue(id), 1e-4f);
        }
    }
}
