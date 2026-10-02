using System.Text.RegularExpressions;
using Akiron.Identity.Domain.Common;

namespace Akiron.Identity.Domain.Users;

/// <summary>
/// An email address, normalised so that one mailbox is one account.
/// </summary>
/// <remarks>
/// Lower-cased with invariant rules. ToLower() under tr-TR turns "I" into a dotless
/// "ı", so "INFO@X.COM" typed on a Turkish machine would become a different address
/// from the same text on the server — and the unique index would let both register.
/// The format check is deliberately loose: the only real test of an address is a mail
/// arriving there, which is confirmation, not a regex.
/// </remarks>
public sealed partial record Email
{
    /// <summary>The longest address SMTP allows (RFC 5321).</summary>
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant() ?? string.Empty;

        if (value.Length > MaxLength)
        {
            throw new DomainValidationException(
                IdentityErrorCodes.EmailTooLong,
                $"Email must be at most {MaxLength} characters, but was {value.Length}.",
                new Dictionary<string, object?> { ["maxLength"] = MaxLength });
        }

        if (!EmailPattern().IsMatch(value))
        {
            throw new DomainValidationException(
                IdentityErrorCodes.EmailInvalidFormat,
                "Email must look like name@domain.tld.");
        }

        return new Email(value);
    }

    public override string ToString() => Value;

    // One @, something on each side, a dot in the domain, no whitespace.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
