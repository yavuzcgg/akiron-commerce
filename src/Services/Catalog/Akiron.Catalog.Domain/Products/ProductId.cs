using System.Diagnostics.CodeAnalysis;

namespace Akiron.Catalog.Domain.Products;

/// <summary>
/// Identifies a <see cref="Product"/>. Same shape as
/// <see cref="Categories.CategoryId"/>: a distinct type, so handing a category id to
/// something expecting a product fails to compile.
/// </summary>
public readonly record struct ProductId(Guid Value) : IParsable<ProductId>
{
    /// <summary>Version 7 is time-ordered, so the key index appends instead of splitting pages.</summary>
    public static ProductId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ProductId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ProductId result)
    {
        if (Guid.TryParse(s, provider, out var guid))
        {
            result = new ProductId(guid);
            return true;
        }

        result = default;
        return false;
    }
}
