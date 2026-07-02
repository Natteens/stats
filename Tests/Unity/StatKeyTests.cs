using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Stats;
using Stats.Editor;

namespace Stats.Tests
{
    public sealed class StatKeyTests
    {
        static StatDefinition Def(string displayName, string key)
        {
            var def = ScriptableObject.CreateInstance<StatDefinition>();
            def.name = displayName;
            typeof(StatDefinition).GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(def, null);
            SetPrivate(def, "displayName", displayName);
            if (key != null) SetPrivate(def, "key", key);
            return def;
        }

        static void SetPrivate(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        static StatSheetPreset Preset(params StatSheetPreset.Entry[] entries)
        {
            var preset = ScriptableObject.CreateInstance<StatSheetPreset>();
            SetPrivate(preset, "entries", entries);
            return preset;
        }

        static StatSheetPreset.Entry E(StatDefinition d, float baseValue) =>
            new StatSheetPreset.Entry { stat = d, baseValue = baseValue };

        static StatSheetBehaviour Behaviour(StatSheetPreset preset)
        {
            var go = new GameObject("entity");
            var b = go.AddComponent<StatSheetBehaviour>();
            SetPrivate(b, "preset", preset);
            typeof(StatSheetBehaviour).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(b, null);
            return b;
        }

        [Test]
        public void Preset_TryGetStatIdByKey_ReturnsCorrectId()
        {
            var def = Def("Movement Speed", "movement_speed");
            var preset = Preset(E(def, 5f));
            Assert.IsTrue(preset.TryGetStatIdByKey("movement_speed", out var id));
            Assert.AreEqual(def.ToStatId(), id);
            Object.DestroyImmediate(def); Object.DestroyImmediate(preset);
        }

        [Test]
        public void Behaviour_LookupByKey_ReturnsValueAndFallback()
        {
            var def = Def("Max Health", "max_health");
            var b = Behaviour(Preset(E(def, 100f)));
            Assert.IsTrue(b.TryGetStatId("max_health", out var id));
            Assert.AreEqual(def.ToStatId(), id);
            Assert.IsTrue(b.TryGetValue("max_health", out float v));
            Assert.AreEqual(100f, v, 1e-4f);
            Assert.AreEqual(100f, b.GetValue("max_health"), 1e-4f);
            Assert.AreEqual(7f, b.GetValue("ghost", 7f), 1e-4f);
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def);
        }

        [Test]
        public void DisplayNameChange_DoesNotBreakKeyLookup()
        {
            var def = Def("Jump Height", "jump_height");
            var b = Behaviour(Preset(E(def, 2f)));
            SetPrivate(def, "displayName", "Totally Different Label");
            b.RebuildKeyCache();
            Assert.IsTrue(b.TryGetStatId("jump_height", out _));
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def);
        }

        [Test]
        public void EmptyKey_DoesNotBreakBuild_AndIsNotFoundByKey()
        {
            var def = Def("No Key Stat", "");
            var preset = Preset(E(def, 3f));
            var sheet = preset.BuildSheet();
            Assert.IsTrue(sheet.IsRegistered(def.ToStatId()));
            Assert.IsFalse(preset.TryGetDefinitionByKey("", out _));
            var b = Behaviour(preset);
            Assert.IsFalse(b.TryGetStatId("", out _));
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def); Object.DestroyImmediate(preset);
        }

        [Test]
        public void UnknownKey_ReturnsFalse_AddThrows()
        {
            var def = Def("Attack", "attack");
            var b = Behaviour(Preset(E(def, 10f)));
            Assert.IsFalse(b.TryGetStatId("nope", out _));
            Assert.Throws<KeyNotFoundException>(() => b.AddFlat("nope", 1f, "src"));
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def);
        }

        [Test]
        public void DuplicateKey_DetectedAndCacheKeepsFirstDeterministically()
        {
            var a = Def("Speed A", "speed");
            var bDef = Def("Speed B", "speed");
            var entries = new[] { E(a, 1f), E(bDef, 2f) };
            Assert.AreEqual(1, PresetValidation.FindDuplicateKeys(entries).Count);
            var behaviour = Behaviour(Preset(entries));
            Assert.IsTrue(behaviour.TryGetStatId("speed", out var id));
            Assert.AreEqual(a.ToStatId(), id);
            Object.DestroyImmediate(behaviour.gameObject); Object.DestroyImmediate(a); Object.DestroyImmediate(bDef);
        }

        [Test]
        public void AddFlatByKey_ModifiesCorrectStat()
        {
            var def = Def("Attack", "attack");
            var b = Behaviour(Preset(E(def, 10f)));
            b.AddFlat("attack", 5f, "buff");
            Assert.AreEqual(15f, b.GetValue("attack"), 1e-4f);
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def);
        }

        [Test]
        public void StatIdApi_StillWorks()
        {
            var def = Def("Attack", "attack");
            var b = Behaviour(Preset(E(def, 10f)));
            var id = def.ToStatId();
            b.Sheet.AddFlat(id, 3f, "src");
            Assert.AreEqual(13f, b.Sheet.GetValue(id), 1e-4f);
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def);
        }

        [Test]
        public void SaveRestore_UsesStatId_NotKey()
        {
            var def = Def("Max Health", "max_health");
            var b = Behaviour(Preset(E(def, 100f)));
            b.AddFlat("max_health", 50f, "ring");
            var data = b.CaptureSaveData();
            Assert.AreEqual(def.ToStatId().ToString(), data.bases[0].statId);
            foreach (var m in data.modifiers) m.statKey = null;
            b.Sheet.ClearModifiers(def.ToStatId());
            b.Sheet.SetBaseValue(def.ToStatId(), 1f);
            b.RestoreSaveData(data);
            Assert.AreEqual(150f, b.GetValue("max_health"), 1e-4f);
            Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(def);
        }

        [Test]
        public void StatKey_NormalizeAndValidate()
        {
            Assert.AreEqual("movement_speed", StatKey.Normalize("Movement Speed"));
            Assert.AreEqual("max_jumps", StatKey.Normalize("  Max  Jumps! "));
            Assert.IsTrue(StatKey.IsValid("movement_speed"));
            Assert.IsFalse(StatKey.IsValid("Movement Speed"));
            Assert.IsFalse(StatKey.IsValid("bad__key"));
            Assert.IsFalse(StatKey.IsValid("_lead"));
            Assert.IsFalse(StatKey.IsValid(""));
        }
    }
}
