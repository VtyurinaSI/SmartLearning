namespace StrategyPatternExample;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money FromEuros(decimal amount) => new(amount, "EUR");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new Money(decimal.Round(Amount * factor, 2), Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}");
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
