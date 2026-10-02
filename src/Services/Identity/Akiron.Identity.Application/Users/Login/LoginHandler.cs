using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Identity.Application.Users.Login;

/// <summary>
/// Exchanges credentials for an access token, without revealing which part was wrong —
/// not in the response, and not in how long the response takes.
/// </summary>
public sealed class LoginHandler(
    IIdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider)
{
    /// <summary>
    /// A real hash of a password nobody has, made once per process. Verifying against it
    /// costs exactly what verifying a real account costs.
    /// </summary>
    private static string? s_dummyHash;

    public async Task<AccessTokenResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null)
        {
            // Measured before this line existed: a wrong password took a median 42.5 ms
            // (100k PBKDF2 iterations), an unknown email 3.0 ms (one indexed lookup). The
            // bodies were identical; the clock told an attacker which emails have accounts.
            // Doing the same hashing work here closes that gap. The race to fill the field
            // is harmless: at worst two threads each compute an equally good dummy hash.
            s_dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString());
            _ = passwordHasher.Verify(s_dummyHash, request.Password!);

            throw IdentityErrors.InvalidCredentials();
        }

        var check = passwordHasher.Verify(user.PasswordHash, request.Password!);

        if (check == PasswordCheck.Failed)
        {
            throw IdentityErrors.InvalidCredentials();
        }

        if (check == PasswordCheck.SuccessRehashNeeded)
        {
            user.UpgradePasswordHash(passwordHasher.Hash(request.Password!));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var token = tokenIssuer.Issue(user);
        var expiresIn = (int)Math.Round((token.ExpiresAt - timeProvider.GetUtcNow()).TotalSeconds);

        return new AccessTokenResponse(token.Value, "Bearer", expiresIn);
    }
}
