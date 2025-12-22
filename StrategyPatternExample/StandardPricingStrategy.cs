namespace StrategyPatternExample;

public sealed class StandardPricingStrategy : IPricingStrategy
{
    public Money CalculateTotal(Money baseAmount, CustomerTier customerTier) => baseAmount;
}
