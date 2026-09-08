using Akiron.Catalog.Domain.Categories;

namespace Akiron.Catalog.Application.Categories;

/// <summary>
/// What the API returns for a category. Separate from the entity so that renaming a
/// private field never silently changes the public contract.
/// </summary>
public sealed record CategoryResponse(
    CategoryId Id,
    string Name,
    string Slug,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static CategoryResponse From(Category category) =>
        new(category.Id, category.Name, category.Slug.Value, category.CreatedAt, category.UpdatedAt);
}
