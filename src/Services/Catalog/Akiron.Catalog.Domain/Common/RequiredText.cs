namespace Akiron.Catalog.Domain.Common;

/// <summary>
/// Trims and length-checks the free text fields entities carry.
/// </summary>
/// <remarks>
/// Extracted when the second entity needed it. The rules are identical everywhere
/// (trim, reject empty, cap the length); only the limit and the field name change,
/// so keeping one copy stops the wording of these errors drifting apart.
/// </remarks>
public static class RequiredText
{
    public static string Normalise(string? value, int maxLength, string fieldName)
    {
        var text = value?.Trim() ?? string.Empty;

        return text.Length switch
        {
            0 => throw new DomainValidationException(
                CatalogErrorCodes.TextRequired,
                $"{fieldName} must not be empty.",
                new Dictionary<string, object?> { ["field"] = fieldName }),
            var length when length > maxLength => throw new DomainValidationException(
                CatalogErrorCodes.TextTooLong,
                $"{fieldName} must be at most {maxLength} characters, but was {length}.",
                new Dictionary<string, object?> { ["field"] = fieldName, ["maxLength"] = maxLength }),
            _ => text,
        };
    }

    /// <summary>Same, but an absent or blank value collapses to null instead of throwing.</summary>
    public static string? NormaliseOptional(string? value, int maxLength, string fieldName)
    {
        var text = value?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.Length > maxLength)
        {
            throw new DomainValidationException(
                CatalogErrorCodes.TextTooLong,
                $"{fieldName} must be at most {maxLength} characters, but was {text.Length}.",
                new Dictionary<string, object?> { ["field"] = fieldName, ["maxLength"] = maxLength });
        }

        return text;
    }
}
