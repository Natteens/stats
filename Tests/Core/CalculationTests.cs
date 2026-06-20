using NUnit.Framework;
using Stats;

namespace Stats.Tests
{
    public sealed class CalculationTests
    {
        static StatId Str() => StatId.NewId();

        [Test]
        public void Flat_AddsToBase()
        {
            var s = new StatSheet();
            var id = Str();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 5f, "sword");
            Assert.AreEqual(15f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void PercentAdd_StacksAdditively()
        {
            var s = new StatSheet();
            var id = Str();
            s.RegisterStat(id, 100f);
            s.AddPercentAdd(id, 0.2f, "a");
            s.AddPercentAdd(id, 0.2f, "b");
            Assert.AreEqual(140f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void PercentMult_StacksMultiplicatively()
        {
            var s = new StatSheet();
            var id = Str();
            s.RegisterStat(id, 100f);
            s.AddPercentMult(id, 0.2f, "a");
            s.AddPercentMult(id, 0.2f, "b");
            Assert.AreEqual(144f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void Combined_Equals_31_875()
        {
            var s = new StatSheet();
            var id = Str();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 5f, "sword");
            s.AddFlat(id, 2f, "ring");
            s.AddPercentAdd(id, 0.5f, "rage");
            s.AddPercentMult(id, 0.25f, "shrine");
            Assert.AreEqual(31.875f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void Override_ReplacesResult_LastWins()
        {
            var s = new StatSheet();
            var id = Str();
            s.RegisterStat(id, 10f);
            s.AddFlat(id, 100f, "flat");
            s.AddOverride(id, 5f, "first");
            s.AddOverride(id, 8f, "second");
            Assert.AreEqual(8f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void Clamp_AppliesAsLastPhase()
        {
            var s = new StatSheet();
            var id = Str();
            s.RegisterStat(id, 0.1f, 0f, 1f);
            s.AddFlat(id, 1.5f, "buff");
            Assert.AreEqual(1f, s.GetValue(id), 1e-4f);
        }

        [Test]
        public void Cache_RecomputesOnlyWhenDirty()
        {
            var counter = new CountingCalculator(PipelineStatCalculator.CreateDefault());
            var s = new StatSheet(counter);
            var id = Str();
            s.RegisterStat(id, 10f);
            _ = s.GetValue(id);
            _ = s.GetValue(id);
            Assert.AreEqual(1, counter.Count);
            s.AddFlat(id, 5f, "src");
            _ = s.GetValue(id);
            Assert.AreEqual(2, counter.Count);
        }

        sealed class CountingCalculator : IStatCalculator
        {
            readonly IStatCalculator inner;
            public int Count;
            public CountingCalculator(IStatCalculator inner) => this.inner = inner;
            public float Calculate(in CalculationContext context)
            {
                Count++;
                return inner.Calculate(context);
            }
        }
    }
}
