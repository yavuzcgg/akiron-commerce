namespace Akiron.Identity.Infrastructure.Security;

/// <summary>What every token says about where it came from and who it is for.</summary>
/// <param name="Issuer">The <c>iss</c> claim; validators reject tokens from anyone else.</param>
/// <param name="Audience">The <c>aud</c> claim; one audience for the whole platform until a service needs its own.</param>
/// <param name="Lifetime">
/// Fifteen minutes (ADR-0011): long enough not to refresh constantly, short enough that a
/// stolen token stops working soon. Revocation before expiry is what refresh families
/// and server-side permission checks are for.
/// </param>
public sealed record TokenSettings(string Issuer, string Audience, TimeSpan Lifetime)
{
    public static readonly TokenSettings Default = new("akiron-identity", "akiron", TimeSpan.FromMinutes(15));
}
