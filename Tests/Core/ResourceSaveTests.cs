using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class ResourceSaveTests
    {
        [Test]
        public void Capture_Restore_CurrentValue()
        {
            var s = new StatSheet();
            var maxHealth = StatId.NewId();
            s.RegisterStat(maxHealth, 100f);
            var r = new RuntimeResource(s, maxHealth);
            r.SetCurrent(70f);

            var data = ResourceSaveInterop.Capture("hp", r);
            Assert.AreEqual(70f, data.current, 1e-4f);
            Assert.AreEqual(maxHealth.ToString(), data.maxStatId);

            var s2 = new StatSheet();
            s2.RegisterStat(maxHealth, 100f);
            var r2 = new RuntimeResource(s2, maxHealth);
            ResourceSaveInterop.Restore(r2, data);
            Assert.AreEqual(70f, r2.Current, 1e-4f);
        }

        [Test]
        public void Restore_Current_AfterMaxStatModifierRestored()
        {
            var s = new StatSheet();
            var maxHealth = StatId.NewId();
            s.RegisterStat(maxHealth, 100f);
            s.AddFlat(maxHealth, 50f, "ring");
            var r = new RuntimeResource(s, maxHealth);
            r.SetCurrent(120f);

            var sheetData = StatSaveInterop.Capture(s);
            var resData = ResourceSaveInterop.Capture("hp", r);

            var s2 = new StatSheet();
            s2.RegisterStat(maxHealth, 100f);
            var r2 = new RuntimeResource(s2, maxHealth);
            StatSaveInterop.Restore(s2, sheetData);
            ResourceSaveInterop.Restore(r2, resData);

            Assert.AreEqual(150f, r2.Max, 1e-4f);
            Assert.AreEqual(120f, r2.Current, 1e-4f);
        }
    }
}
