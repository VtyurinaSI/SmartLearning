namespace StrategyPatternExample;

public interface IPricingStrategy
{
    Money CalculateTotal(Money baseAmount, CustomerTier customerTier);
}
