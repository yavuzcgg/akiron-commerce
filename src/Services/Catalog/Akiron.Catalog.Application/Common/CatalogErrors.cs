using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Application.Common;

/// <summary>
/// Every failure a handler can raise, built in one place.
/// </summary>
/// <remarks>
/// A factory per failure rather than exception constructors scattered through the
/// handlers: the code, the English message and the parameters a client needs are then
/// defined once and stay in step. Reading this file is also the fastest way to see
/// what this service can refuse to do.
/// </remarks>
public static class CatalogErrors
{
    public static NotFoundException CategoryNotFound(CategoryId categoryId) => new(
        CatalogErrorCodes.CategoryNotFound,
        $"Category '{categoryId}' was not found.",
        new Dictionary<string, object?> { ["categoryId"] = categoryId.Value });

    public static ConflictException CategorySlugTaken(Slug slug) => new(
        CatalogErrorCodes.CategorySlugConflict,
        $"A category with slug '{slug}' already exists.",
        new Dictionary<string, object?> { ["slug"] = slug.Value });

    public static ConflictException CategoryHasProducts() => new(
        CatalogErrorCodes.CategoryHasProducts,
        "This category still has products. Move or delete them before deleting the category.");

    public static NotFoundException ProductNotFound(ProductId productId) => new(
        CatalogErrorCodes.ProductNotFound,
        $"Product '{productId}' was not found.",
        new Dictionary<string, object?> { ["productId"] = productId.Value });

    public static ConflictException ProductSkuTaken(Sku sku) => new(
        CatalogErrorCodes.ProductSkuConflict,
        $"A product with SKU '{sku}' already exists.",
        new Dictionary<string, object?> { ["sku"] = sku.Value });

    /// <summary>Raised when a unique index fires on a constraint no handler pre-checked.</summary>
    public static ConflictException UnexpectedConflict() => new(
        CatalogErrorCodes.Conflict,
        "The request conflicts with data that already exists.");
}
