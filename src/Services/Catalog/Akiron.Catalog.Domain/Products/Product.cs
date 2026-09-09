using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Pricing;

namespace Akiron.Catalog.Domain.Products;

/// <summary>
/// A sellable item in the catalogue.
/// </summary>
/// <remarks>
/// <see cref="BasePrice"/> is the list price. What a given dealer pays is that price
/// run through their price group and markup chain, resolved per request and never
/// stored on the product (ADR-0012).
/// </remarks>
public sealed class Product
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;

    // EF materialisation only; these nulls are overwritten as the row is read.
    private Product()
    {
        Sku = null!;
        Name = null!;
        BasePrice = null!;
    }

    private Product(
        ProductId id,
        Sku sku,
        string name,
        string? description,
        CategoryId categoryId,
        Money basePrice,
        DateTimeOffset createdAt)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        CategoryId = categoryId;
        BasePrice = basePrice;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public ProductId Id { get; private set; }

    /// <summary>
    /// Immutable after creation. The SKU is the reference ERPs and marketplaces store
    /// on their side, so changing it here would silently break theirs.
    /// </summary>
    public Sku Sku { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public CategoryId CategoryId { get; private set; }

    public Money BasePrice { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Product Create(
        Sku sku,
        string? name,
        string? description,
        CategoryId categoryId,
        Money basePrice) =>
        new(ProductId.New(),
            sku,
            RequiredText.Normalise(name, MaxNameLength, "Product name"),
            RequiredText.NormaliseOptional(description, MaxDescriptionLength, "Product description"),
            categoryId,
            basePrice,
            Timestamp.UtcNow());

    public void Update(string? name, string? description, CategoryId categoryId, Money basePrice, bool isActive)
    {
        Name = RequiredText.Normalise(name, MaxNameLength, "Product name");
        Description = RequiredText.NormaliseOptional(description, MaxDescriptionLength, "Product description");
        CategoryId = categoryId;
        BasePrice = basePrice;
        IsActive = isActive;
        UpdatedAt = Timestamp.UtcNow();
    }
}
