using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>
/// The agreed price of one product for one price group — the override that beats the
/// group discount.
/// </summary>
/// <remarks>
/// Keyed on the pair it describes rather than a surrogate id: a group cannot hold two
/// prices for the same product, and making that the primary key means the database
/// guarantees it without a separate unique index to keep in step.
/// </remarks>
public sealed class PriceListEntry
{
    private PriceListEntry() => Price = null!;

    private PriceListEntry(PriceGroupId priceGroupId, ProductId productId, Money price, DateTimeOffset createdAt)
    {
        PriceGroupId = priceGroupId;
        ProductId = productId;
        Price = price;
        CreatedAt = createdAt;
    }

    public PriceGroupId PriceGroupId { get; private set; }

    public ProductId ProductId { get; private set; }

    public Money Price { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// The price must be in the group's currency. The caller never sends one — it is
    /// read off the group — so this guard catches a programming mistake rather than bad
    /// input.
    /// </summary>
    public static PriceListEntry Create(PriceGroup priceGroup, ProductId productId, Money price)
    {
        EnsureCurrencyMatches(priceGroup, price);

        return new PriceListEntry(priceGroup.Id, productId, price, Timestamp.UtcNow());
    }

    public void ChangePrice(PriceGroup priceGroup, Money price)
    {
        EnsureCurrencyMatches(priceGroup, price);

        Price = price;
        UpdatedAt = Timestamp.UtcNow();
    }

    private static void EnsureCurrencyMatches(PriceGroup priceGroup, Money price)
    {
        if (price.Currency != priceGroup.Currency)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.PriceCurrencyMismatch,
                $"Price group '{priceGroup.Code}' is in {priceGroup.Currency}, but the price is in {price.Currency}.",
                new Dictionary<string, object?>
                {
                    ["priceGroupCurrency"] = priceGroup.Currency.ToString(),
                    ["priceCurrency"] = price.Currency.ToString(),
                });
        }
    }
}
