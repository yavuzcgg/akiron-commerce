using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Categories;

/// <summary>
/// A product category. Flat for now: a parent/child tree is only worth its
/// complexity once the storefront navigation needs it (Faz 2).
/// </summary>
public sealed class Category
{
    public const int MaxNameLength = 200;

    // EF materialisation only. The nulls are immediately overwritten by the
    // property setters EF calls, which is why they are forgiven here.
    private Category()
    {
        Name = null!;
        Slug = null!;
    }

    private Category(CategoryId id, string name, Slug slug, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Slug = slug;
        CreatedAt = createdAt;
    }

    public CategoryId Id { get; private set; }

    public string Name { get; private set; }

    public Slug Slug { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Category Create(string? name, Slug slug) =>
        new(CategoryId.New(), NormaliseName(name), slug, Timestamp.UtcNow());

    public void Rename(string? name)
    {
        Name = NormaliseName(name);
        UpdatedAt = Timestamp.UtcNow();
    }

    /// <summary>
    /// The API validates names too, with friendlier messages. This guard is the
    /// backstop: an entity must not be constructible in an invalid state, whatever
    /// the caller forgot to check.
    /// </summary>
    private static string NormaliseName(string? name) =>
        RequiredText.Normalise(name, MaxNameLength, "Category name");
}
