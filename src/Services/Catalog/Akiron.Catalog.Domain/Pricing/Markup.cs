using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>
/// What a dealer adds on top of the price they pay — 10 means ten percent.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="DiscountPercentage"/> even though the validation is
/// identical. They mean opposite things: one comes off a price and the other goes on
/// top, and a separate type is what stops one being passed where the other belongs —
/// the same reasoning that makes ProductId a different type from CategoryId.
/// </remarks>
public sealed record Markup
{
    public const int DecimalPlaces = 2;

    private Markup(decimal value) => Value = value;

    public decimal Value { get; }

    public static Markup Create(decimal value)
    {
        // Negative is not a discount by another name: markups run down a chain of
        // sub-dealers and letting one subtract would mean a dealer could sell below the
        // price they were given without anyone noticing.
        if (value is < 0m or > 100m)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.MarkupOutOfRange,
                $"Markup must be between 0 and 100, but was {value}.",
                new Dictionary<string, object?> { ["markup"] = value });
        }

        if (decimal.Round(value, DecimalPlaces) != value)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.MarkupOutOfRange,
                $"Markup {value} has more than {DecimalPlaces} decimal places.",
                new Dictionary<string, object?> { ["markup"] = value, ["decimalPlaces"] = DecimalPlaces });
        }

        return new Markup(value);
    }

    /// <summary>The multiplier this markup applies: 10% becomes 1.10.</summary>
    public decimal Multiplier => 1m + (Value / 100m);

    public override string ToString() => $"+{Value}%";
}
