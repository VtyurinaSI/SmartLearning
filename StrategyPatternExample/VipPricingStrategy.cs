namespace StrategyPatternExample;

public sealed class VipPricingStrategy : IPricingStrategy
{
    private readonly decimal _vipDiscountFactor;

    public VipPricingStrategy(decimal vipDiscountFactor = 0.90m)
    {
        if (vipDiscountFactor <= 0m || vipDiscountFactor > 1m)
            throw new ArgumentOutOfRangeException(nameof(vipDiscountFactor), "Factor must be in (0, 1].");

        _vipDiscountFactor = vipDiscountFactor;
    }

    public Money CalculateTotal(Money baseAmount, CustomerTier customerTier)
    {
        return customerTier == CustomerTier.Vip
            ? baseAmount.Multiply(_vipDiscountFactor)
            : baseAmount;
    }
}
