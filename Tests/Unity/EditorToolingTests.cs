using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Stats;
using Stats.Editor;

namespace Stats.Tests
{
    public sealed class EditorToolingTests
    {
        static StatDefinition Def(string name)
        {
            var def = ScriptableObject.CreateInstance<StatDefinition>();
            def.name = name;
            Validate(def);
            return def;
        }

        static void Validate(StatDefinition def)
        {
            typeof(StatDefinition).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(def, null);
        }

        static void SetId(StatDefinition def, string id)
        {
            typeof(StatDefinition).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(def, id);
        }

        [Test]
        public void FindMissingIds_DetectsEmpty()
        {
            var a = ScriptableObject.CreateInstance<StatDefinition>();
            var b = Def("B");
            var missing = StatIdValidator.FindMissingIds(new[] { a, b });
            Assert.Contains(a, missing);
            Assert.IsFalse(missing.Contains(b));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void FindDuplicateIds_DetectsSharedId()
        {
            var a = Def("A");
            var b = Def("B");
            var c = Def("C");
            SetId(b, a.Id.ToString());
            var dup = StatIdValidator.FindDuplicateIds(new[] { a, b, c });
            Assert.Contains(a, dup);
            Assert.Contains(b, dup);
            Assert.IsFalse(dup.Contains(c));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void PercentConversion_RoundTrips()
        {
            Assert.AreEqual(50f, PercentConversion.ToDisplay(0.5f, ModifierOperation.PercentAdd), 1e-4f);
            Assert.AreEqual(0.5f, PercentConversion.ToStored(50f, ModifierOperation.PercentMult), 1e-4f);
            Assert.AreEqual(5f, PercentConversion.ToDisplay(5f, ModifierOperation.Flat), 1e-4f);
            Assert.AreEqual(5f, PercentConversion.ToStored(5f, ModifierOperation.Override), 1e-4f);
            Assert.IsTrue(PercentConversion.IsPercent(ModifierOperation.PercentAdd));
            Assert.IsFalse(PercentConversion.IsPercent(ModifierOperation.Flat));
        }

        [Test]
        public void PresetValidation_DetectsNullAndDuplicate()
        {
            var a = Def("A");
            var entries = new[]
            {
                new StatSheetPreset.Entry { stat = a, baseValue = 1f },
                new StatSheetPreset.Entry { stat = a, baseValue = 2f },
                new StatSheetPreset.Entry { stat = null, baseValue = 3f }
            };
            Assert.AreEqual(1, PresetValidation.FindNullEntries(entries).Count);
            Assert.Contains(a, PresetValidation.FindDuplicateStats(entries));
            Object.DestroyImmediate(a);
        }

        [Test]
        public void PresetValidation_DetectsMinGreaterThanMax()
        {
            var e = new StatSheetPreset.Entry { overrideMin = true, min = 10f, overrideMax = true, max = 5f };
            Assert.IsTrue(PresetValidation.HasMinGreaterThanMax(e));
            var ok = new StatSheetPreset.Entry { overrideMin = true, min = 1f, overrideMax = true, max = 5f };
            Assert.IsFalse(PresetValidation.HasMinGreaterThanMax(ok));
        }
    }
}
