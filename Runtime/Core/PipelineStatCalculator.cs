using System.Collections.Generic;

namespace Stats
{
    public sealed class PipelineStatCalculator : IStatCalculator
    {
        readonly ICalculationStep[] steps;
        public PipelineStatCalculator(IEnumerable<ICalculationStep> steps)
        {
            var list = new List<ICalculationStep>(steps);
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            this.steps = list.ToArray();
        }
        public float Calculate(in CalculationContext context)
        {
            float current = context.Base;
            for (int i = 0; i < steps.Length; i++)
                current = steps[i].Apply(current, context);
            return current;
        }
        public static PipelineStatCalculator CreateDefault() =>
            new PipelineStatCalculator(new ICalculationStep[]
            {
                new FlatStep(),
                new PercentAddStep(),
                new PercentMultStep(),
                new OverrideStep(),
                new ClampStep()
            });
    }
}
