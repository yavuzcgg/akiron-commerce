using System.Text.RegularExpressions;
using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Products;

/// <summary>
/// A stock keeping unit such as MICH-205-55-R16 — the catalogue's business
/// identifier, and the value ERPs and marketplaces key on.
/// </summary>
/// <remarks>
/// Normalised to upper case on construction, so abc-1 and ABC-1 are one SKU. Without
/// that the unique index would accept both and the catalogue would carry two rows for
/// one product.
/// </remarks>
public sealed partial record Sku
{
    public const int MinLength = 3;
    public const int MaxLength = 64;

    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Create(string? raw)
    {
        // ToUpperInvariant, never ToUpper(): under tr-TR the culture-aware version maps
        // i to a dotted capital, so the same SKU typed on a Turkish machine would
        // normalise differently than on the server.
        var value = raw?.Trim().ToUpperInvariant() ?? string.Empty;

        if (value.Length is < MinLength or > MaxLength)
        {
            throw new DomainValidationException(
                $"SKU must be between {MinLength} and {MaxLength} characters, but was {value.Length}.");
        }

        if (!SkuPattern().IsMatch(value))
        {
            throw new DomainValidationException(
                $"SKU '{value}' is invalid: use letters, digits and single hyphens, not starting or ending with a hyphen.");
        }

        return new Sku(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]+(-[A-Z0-9]+)*$")]
    private static partial Regex SkuPattern();
}
