namespace Stats
{
    public interface ICalculationStep
    {
        int Order { get; }
        float Apply(float currentValue, in CalculationContext context);
    }
}
