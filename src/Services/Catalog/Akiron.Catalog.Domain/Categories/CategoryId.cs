using System.Diagnostics.CodeAnalysis;

namespace Akiron.Catalog.Domain.Categories;

/// <summary>
/// Identifies a <see cref="Category"/>. A distinct type rather than a bare Guid so
/// that passing a ProductId where a CategoryId belongs fails to compile instead of
/// failing in production.
/// </summary>
/// <remarks>
/// Implementing <see cref="IParsable{TSelf}"/> is what lets minimal APIs bind it
/// straight from a route segment — no custom model binder needed.
/// </remarks>
public readonly record struct CategoryId(Guid Value) : IParsable<CategoryId>
{
    /// <summary>
    /// Version 7 GUIDs embed a timestamp, so generated keys land in ascending order
    /// and PostgreSQL's B-tree index appends instead of splitting pages at random.
    /// </summary>
    public static CategoryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static CategoryId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CategoryId result)
    {
        if (Guid.TryParse(s, provider, out var guid))
        {
            result = new CategoryId(guid);
            return true;
        }

        result = default;
        return false;
    }
}
