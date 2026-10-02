using Akiron.Identity.Domain.Common;

namespace Akiron.Identity.Domain.Users;

/// <summary>
/// Someone who can sign in.
/// </summary>
/// <remarks>
/// The entity holds a password <em>hash</em> and never sees a password: hashing needs a
/// library (ADR-0019) and the domain references nothing, so the Application layer hashes
/// and hands the result in. Dealers, price groups and markups join this model in 2.4.
/// </remarks>
public sealed class User
{
    public const int MaxDisplayNameLength = 100;

    private User()
    {
        Email = null!;
        DisplayName = null!;
        PasswordHash = null!;
    }

    private User(UserId id, Email email, string displayName, string passwordHash, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public UserId Id { get; private set; }

    public Email Email { get; private set; }

    public string DisplayName { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static User Register(Email email, string? displayName, string passwordHash) =>
        new(UserId.New(), email, NormaliseDisplayName(displayName), RequireHash(passwordHash), Timestamp.UtcNow());

    /// <summary>
    /// Replaces the stored hash with one made at the current work factor, after a
    /// successful login reported the old one as outdated.
    /// </summary>
    public void UpgradePasswordHash(string passwordHash)
    {
        PasswordHash = RequireHash(passwordHash);
        UpdatedAt = Timestamp.UtcNow();
    }

    private static string NormaliseDisplayName(string? displayName)
    {
        var text = displayName?.Trim() ?? string.Empty;

        return text.Length switch
        {
            0 => throw new DomainValidationException(
                IdentityErrorCodes.TextRequired,
                "Display name must not be empty.",
                new Dictionary<string, object?> { ["field"] = "Display name" }),
            > MaxDisplayNameLength => throw new DomainValidationException(
                IdentityErrorCodes.TextTooLong,
                $"Display name must be at most {MaxDisplayNameLength} characters, but was {text.Length}.",
                new Dictionary<string, object?> { ["field"] = "Display name", ["maxLength"] = MaxDisplayNameLength }),
            _ => text,
        };
    }

    // A programming guard, not input validation: an empty hash would make the account
    // impossible to log in to, or — with a careless verifier — possible for anyone.
    private static string RequireHash(string passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? throw new ArgumentException("A password hash is required.", nameof(passwordHash))
            : passwordHash;
}
