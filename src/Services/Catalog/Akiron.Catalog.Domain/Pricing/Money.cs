using System.Globalization;
using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>An amount in a currency. Never negative, never finer than minor units.</summary>
public sealed record Money
{
    public const int DecimalPlaces = 2;

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Money Create(decimal amount, Currency currency)
    {
        if (!Enum.IsDefined(currency))
        {
            throw new DomainValidationException($"Currency '{currency}' is not supported.");
        }

        if (amount < 0)
        {
            throw new DomainValidationException($"Amount must not be negative, but was {amount}.");
        }

        // Rejected, not rounded. Silently dropping a third decimal is the kind of money
        // bug that surfaces months later as a reconciliation mismatch; the caller has to
        // say what the price actually is.
        if (decimal.Round(amount, DecimalPlaces) != amount)
        {
            throw new DomainValidationException(
                $"Amount {amount} has more than {DecimalPlaces} decimal places.");
        }

        return new Money(amount, currency);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:F2} {Currency}");
}
