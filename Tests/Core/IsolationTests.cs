using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class IsolationTests
    {
        [Test]
        public void TwoSheets_SamePreset_DoNotShareState()
        {
            var id = StatId.NewId();
            var a = new StatSheet();
            var b = new StatSheet();
            a.RegisterStat(id, 10f);
            b.RegisterStat(id, 10f);
            a.AddFlat(id, 5f, "src");
            Assert.AreEqual(15f, a.GetValue(id), 1e-4f);
            Assert.AreEqual(10f, b.GetValue(id), 1e-4f);
        }
    }
}
