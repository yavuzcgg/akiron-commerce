using System.Text.RegularExpressions;
using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Domain.Categories;

/// <summary>
/// A URL-safe category identifier such as <c>kis-lastikleri</c>. Validated on
/// construction, so anything holding a <see cref="Slug"/> holds a valid one.
/// </summary>
/// <remarks>
/// A class rather than a record struct on purpose: a struct would let
/// <c>default(Slug)</c> exist with a null value, which is exactly the invalid state
/// this type is meant to make unrepresentable.
/// </remarks>
public sealed partial record Slug
{
    public const int MaxLength = 120;

    private Slug(string value) => Value = value;

    public string Value { get; }

    public static Slug Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            throw new DomainValidationException("Slug must not be empty.");
        }

        if (value.Length > MaxLength)
        {
            throw new DomainValidationException($"Slug must be at most {MaxLength} characters, but was {value.Length}.");
        }

        if (!SlugPattern().IsMatch(value))
        {
            throw new DomainValidationException(
                $"Slug '{value}' is invalid: use lowercase letters, digits and single hyphens, not starting or ending with a hyphen.");
        }

        return new Slug(value);
    }

    public override string ToString() => Value;

    // Lowercase alphanumeric groups joined by single hyphens: "a", "a-b", "kis-lastikleri-2026".
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
