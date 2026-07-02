using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Stats;

namespace Stats.Tests
{
    public sealed class SaveUtilityBehaviourTests
    {
        static StatSheetBehaviour NewBehaviour()
        {
            var go = new GameObject("entity");
            var behaviour = go.AddComponent<StatSheetBehaviour>();
            typeof(StatSheetBehaviour).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(behaviour, null);
            return behaviour;
        }

        [Test]
        public void Behaviour_CaptureRestore_BasesModifiersResources()
        {
            var behaviour = NewBehaviour();
            var maxHealth = StatId.NewId();
            behaviour.Sheet.RegisterStat(maxHealth, 100f);
            behaviour.Sheet.AddFlat(maxHealth, 50f, "ring");
            var resource = new RuntimeResource(behaviour.Sheet, maxHealth);
            resource.SetCurrent(120f);
            behaviour.RegisterResource("hp", resource);

            var data = behaviour.CaptureSaveData();

            behaviour.Sheet.SetBaseValue(maxHealth, 1f);
            behaviour.Sheet.ClearModifiers(maxHealth);
            resource.SetCurrent(1f);

            behaviour.RestoreSaveData(data);

            Assert.AreEqual(150f, behaviour.Sheet.GetValue(maxHealth), 1e-4f);
            Assert.AreEqual(120f, resource.Current, 1e-4f);

            Object.DestroyImmediate(behaviour.gameObject);
        }
    }
}
