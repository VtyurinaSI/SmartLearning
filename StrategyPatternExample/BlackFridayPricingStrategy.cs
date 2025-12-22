namespace StrategyPatternExample;

public sealed class BlackFridayPricingStrategy : IPricingStrategy
{
    private readonly decimal _discountFactor;

    public BlackFridayPricingStrategy(decimal discountFactor = 0.70m)
    {
        if (discountFactor <= 0m || discountFactor > 1m)
            throw new ArgumentOutOfRangeException(nameof(discountFactor), "Factor must be in (0, 1].");

        _discountFactor = discountFactor;
    }

    public Money CalculateTotal(Money baseAmount, CustomerTier customerTier)
        => baseAmount.Multiply(_discountFactor);
}
