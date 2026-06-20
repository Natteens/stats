using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Stats;

namespace Stats.Tests
{
    public sealed class AuthoringTests
    {
        static StatDefinition NewDefinition()
        {
            var def = ScriptableObject.CreateInstance<StatDefinition>();
            InvokePrivate(def, "OnValidate");
            return def;
        }

        static void InvokePrivate(object target, string method)
        {
            var info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            info.Invoke(target, null);
        }

        static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            info.SetValue(target, value);
        }

        [Test]
        public void StatDefinition_GeneratesGuid_WhenMissing()
        {
            var def = ScriptableObject.CreateInstance<StatDefinition>();
            Assert.IsTrue(def.Id.IsEmpty);
            InvokePrivate(def, "OnValidate");
            Assert.IsFalse(def.Id.IsEmpty);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StatDefinition_DoesNotRegenerate_OnRepeatedValidation()
        {
            var def = NewDefinition();
            var first = def.Id;
            InvokePrivate(def, "OnValidate");
            InvokePrivate(def, "OnValidate");
            Assert.AreEqual(first, def.Id);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StatId_Conversion_ReturnsStableGuid()
        {
            var def = NewDefinition();
            Assert.AreEqual(def.ToStatId(), def.ToStatId());
            Assert.IsFalse(def.ToStatId().IsEmpty);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void Preset_BuildsSheet_RegistersAllStats()
        {
            var str = NewDefinition();
            var hp = NewDefinition();
            var preset = ScriptableObject.CreateInstance<StatSheetPreset>();
            var entries = new[]
            {
                new StatSheetPreset.Entry { stat = str, baseValue = 10f },
                new StatSheetPreset.Entry { stat = hp, baseValue = 100f }
            };
            SetPrivate(preset, "entries", entries);

            var sheet = preset.BuildSheet();
            Assert.IsTrue(sheet.IsRegistered(str.ToStatId()));
            Assert.IsTrue(sheet.IsRegistered(hp.ToStatId()));
            Assert.AreEqual(10f, sheet.GetValue(str.ToStatId()), 1e-4f);
            Assert.AreEqual(100f, sheet.GetValue(hp.ToStatId()), 1e-4f);

            Object.DestroyImmediate(str);
            Object.DestroyImmediate(hp);
            Object.DestroyImmediate(preset);
        }

        [Test]
        public void ApplyGroup_AppliesEveryRow_WithSource()
        {
            var str = NewDefinition();
            var sheet = new StatSheet();
            sheet.RegisterStat(str.ToStatId(), 10f);
            var group = new[]
            {
                new StatModifierData { stat = str, operation = ModifierOperation.Flat, value = 5f },
                new StatModifierData { stat = str, operation = ModifierOperation.PercentAdd, value = 0.5f }
            };
            var source = new object();
            sheet.ApplyGroup(group, source);
            Assert.AreEqual(22.5f, sheet.GetValue(str.ToStatId()), 1e-4f);
            Assert.IsTrue(sheet.HasModifierFrom(source));
            Object.DestroyImmediate(str);
        }
    }
}
