using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>
/// How much a price group takes off the list price — 10 means ten percent.
/// </summary>
/// <remarks>
/// Kept as its own type rather than a shared Percentage: the markup chain in Faz 1.5
/// will want the same arithmetic, and that is the point at which a common type earns
/// its place. Extracting one now would be guessing at what the second caller needs.
/// </remarks>
public sealed record DiscountPercentage
{
    public const int DecimalPlaces = 2;

    public static readonly DiscountPercentage None = new(0m);

    private DiscountPercentage(decimal value) => Value = value;

    public decimal Value { get; }

    public bool IsNone => Value == 0m;

    public static DiscountPercentage Create(decimal value)
    {
        if (value is < 0m or > 100m)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.DiscountOutOfRange,
                $"Discount must be between 0 and 100, but was {value}.",
                new Dictionary<string, object?> { ["discountPercentage"] = value });
        }

        // Same rule as Money: a third decimal is refused rather than rounded away, because
        // a discount silently changed by a hundredth of a percent is a discrepancy nobody
        // can trace back later.
        if (decimal.Round(value, DecimalPlaces) != value)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.DiscountOutOfRange,
                $"Discount {value} has more than {DecimalPlaces} decimal places.",
                new Dictionary<string, object?> { ["discountPercentage"] = value, ["decimalPlaces"] = DecimalPlaces });
        }

        return new DiscountPercentage(value);
    }

    public override string ToString() => $"{Value}%";
}
