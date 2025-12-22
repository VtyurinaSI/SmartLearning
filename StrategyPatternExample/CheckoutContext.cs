namespace StrategyPatternExample;

public sealed class CheckoutContext
{
    private IPricingStrategy _strategy;

    public CheckoutContext(IPricingStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public void SetStrategy(IPricingStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public Money CalculateTotal(Money baseAmount, CustomerTier customerTier)
        => _strategy.CalculateTotal(baseAmount, customerTier);
}
