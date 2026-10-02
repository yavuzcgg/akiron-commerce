using System.Diagnostics.CodeAnalysis;

namespace Akiron.Identity.Domain.Users;

/// <summary>Identifies a <see cref="User"/>. Also the token's <c>sub</c> claim.</summary>
public readonly record struct UserId(Guid Value) : IParsable<UserId>
{
    /// <summary>Version 7 is time-ordered, so the key index appends instead of splitting pages.</summary>
    public static UserId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static UserId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UserId result)
    {
        if (Guid.TryParse(s, provider, out var guid))
        {
            result = new UserId(guid);
            return true;
        }

        result = default;
        return false;
    }
}
