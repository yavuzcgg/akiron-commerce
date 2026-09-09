using System.Text.RegularExpressions;
using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Pricing;

/// <summary>
/// Identifies a price group in a way people type and URLs carry: BAYI-A, TOPTAN,
/// PERAKENDE.
/// </summary>
/// <remarks>
/// Upper-cased on construction with invariant rules, exactly like <see cref="Products.Sku"/>:
/// the code appears in URLs, so bayi-a and BAYI-A must be one group rather than two.
/// </remarks>
public sealed partial record PriceGroupCode
{
    public const int MinLength = 2;
    public const int MaxLength = 32;

    private PriceGroupCode(string value) => Value = value;

    public string Value { get; }

    public static PriceGroupCode Create(string? raw)
    {
        // Invariant casing, never ToUpper(): under tr-TR the culture-aware version turns
        // i into a dotted capital, so the same code would normalise differently here than
        // on the server.
        var value = raw?.Trim().ToUpperInvariant() ?? string.Empty;

        if (value.Length is < MinLength or > MaxLength)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.PriceGroupCodeInvalidFormat,
                $"Price group code must be between {MinLength} and {MaxLength} characters, but was {value.Length}.",
                new Dictionary<string, object?> { ["minLength"] = MinLength, ["maxLength"] = MaxLength });
        }

        if (!CodePattern().IsMatch(value))
        {
            throw new DomainValidationException(
                CatalogErrorCodes.PriceGroupCodeInvalidFormat,
                $"Price group code '{value}' is invalid: use letters, digits and single hyphens, not starting or ending with a hyphen.",
                new Dictionary<string, object?> { ["code"] = value });
        }

        return new PriceGroupCode(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]+(-[A-Z0-9]+)*$")]
    private static partial Regex CodePattern();
}
