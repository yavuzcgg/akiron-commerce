using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Akiron.Identity.Infrastructure.Security;

/// <summary>
/// Mints RS256 access tokens.
/// </summary>
/// <remarks>
/// The claims are deliberately few: who (<c>sub</c>), how to greet them (<c>email</c>,
/// <c>name</c>) and a unique id (<c>jti</c>). Anything a service must <em>trust</em> —
/// permissions, prices — is looked up server-side, not read from here (ADR-0012).
/// </remarks>
public sealed class JwtAccessTokenIssuer(
    SigningKeyStore keyStore,
    TokenSettings settings,
    TimeProvider timeProvider) : IAccessTokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken Issue(User user)
    {
        // JWT times are whole seconds; truncating here keeps ExpiresAt equal to the exp
        // claim instead of a few hundred milliseconds later than it.
        var now = DateTimeOffset.FromUnixTimeSeconds(timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var expiresAt = now + settings.Lifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(keyStore.SigningKey, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.Value.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email.Value,
                [JwtRegisteredClaimNames.Name] = user.DisplayName,
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
            },
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAt);
    }
}
