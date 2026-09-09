using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Application.Pricing;

public sealed record PriceGroupResponse(
    PriceGroupId Id,
    string Code,
    string Name,
    string Currency,
    decimal DiscountPercentage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static PriceGroupResponse From(PriceGroup priceGroup) =>
        new(priceGroup.Id,
            priceGroup.Code.Value,
            priceGroup.Name,
            priceGroup.Currency.ToString(),
            priceGroup.Discount.Value,
            priceGroup.CreatedAt,
            priceGroup.UpdatedAt);
}

/// <summary>
/// One agreed price. The currency is echoed back even though it is the group's, because
/// a client reading a single row should not have to fetch the group to know what the
/// number means.
/// </summary>
public sealed record PriceListEntryResponse(
    ProductId ProductId,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static PriceListEntryResponse From(PriceListEntry entry) =>
        new(entry.ProductId,
            entry.Price.Amount,
            entry.Price.Currency.ToString(),
            entry.CreatedAt,
            entry.UpdatedAt);
}
