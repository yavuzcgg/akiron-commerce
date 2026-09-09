using System.Diagnostics.CodeAnalysis;
using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>Identifies a <see cref="PriceGroup"/>.</summary>
public readonly record struct PriceGroupId(Guid Value) : IParsable<PriceGroupId>
{
    public static PriceGroupId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PriceGroupId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PriceGroupId result)
    {
        if (Guid.TryParse(s, provider, out var guid))
        {
            result = new PriceGroupId(guid);
            return true;
        }

        result = default;
        return false;
    }
}

/// <summary>
/// A tier of buyers who share pricing — dealers, wholesale, retail.
/// </summary>
/// <remarks>
/// Two mechanisms, because real dealer systems use both. The group carries a discount
/// off the list price, which scales to a catalogue of any size, and individual products
/// can override it with an agreed price through <see cref="PriceListEntry"/>. Resolution
/// order, implemented in Faz 1.5: the product-specific entry wins, then the group
/// discount, then the list price.
///
/// The currency belongs to the group and every entry inherits it, so a price list cannot
/// end up holding a mixture. A dealer who buys in dollars is assigned a dollar group;
/// converting between currencies is a separate concern with its own exchange rates, and
/// is designed in Faz 2 when Identity brings the dealer's own currency.
/// </remarks>
public sealed class PriceGroup
{
    public const int MaxNameLength = 200;

    private PriceGroup()
    {
        Code = null!;
        Name = null!;
        Discount = null!;
    }

    private PriceGroup(
        PriceGroupId id,
        PriceGroupCode code,
        string name,
        Currency currency,
        DiscountPercentage discount,
        DateTimeOffset createdAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Currency = currency;
        Discount = discount;
        CreatedAt = createdAt;
    }

    public PriceGroupId Id { get; private set; }

    /// <summary>Immutable: the code is what URLs and imports address the group by.</summary>
    public PriceGroupCode Code { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    /// Immutable. Changing it would silently reinterpret every price already in the list
    /// as an amount in a different currency.
    /// </summary>
    public Currency Currency { get; private set; }

    public DiscountPercentage Discount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static PriceGroup Create(
        PriceGroupCode code,
        string? name,
        Currency currency,
        DiscountPercentage discount)
    {
        if (!Enum.IsDefined(currency))
        {
            throw new DomainValidationException(
                CatalogErrorCodes.MoneyUnsupportedCurrency,
                $"Currency '{currency}' is not supported.",
                new Dictionary<string, object?> { ["supported"] = Enum.GetNames<Currency>() });
        }

        return new PriceGroup(
            PriceGroupId.New(),
            code,
            RequiredText.Normalise(name, MaxNameLength, "Price group name"),
            currency,
            discount,
            Timestamp.UtcNow());
    }

    public void Update(string? name, DiscountPercentage discount)
    {
        Name = RequiredText.Normalise(name, MaxNameLength, "Price group name");
        Discount = discount;
        UpdatedAt = Timestamp.UtcNow();
    }
}
