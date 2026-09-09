using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Application.Products;

/// <summary>Money is flattened to amount + currency: the wire format should not mirror our value object.</summary>
public sealed record MoneyResponse(decimal Amount, string Currency)
{
    public static MoneyResponse From(Money money) => new(money.Amount, money.Currency.ToString());
}

public sealed record ProductResponse(
    ProductId Id,
    string Sku,
    string Name,
    string? Description,
    CategoryId CategoryId,
    MoneyResponse BasePrice,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    /// <summary>What the requested price group pays, or null when no group was asked for.</summary>
    MoneyResponse? GroupPrice = null)
{
    public static ProductResponse From(Product product) =>
        new(product.Id,
            product.Sku.Value,
            product.Name,
            product.Description,
            product.CategoryId,
            MoneyResponse.From(product.BasePrice),
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);

    public ProductResponse WithGroupPrice(Money price) => this with { GroupPrice = MoneyResponse.From(price) };
}
