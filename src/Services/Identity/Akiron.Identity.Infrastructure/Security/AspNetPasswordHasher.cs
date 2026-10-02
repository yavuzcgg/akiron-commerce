using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = Akiron.Identity.Application.Common.IPasswordHasher;

namespace Akiron.Identity.Infrastructure.Security;

/// <summary>
/// ASP.NET Core Identity's hasher, and nothing else from that library (ADR-0019).
/// </summary>
/// <remarks>
/// PBKDF2-HMAC-SHA512 with a random salt per password, in a versioned format whose first
/// byte says how it was made — which is how <c>SuccessRehashNeeded</c> knows an old hash
/// when it sees one. The <see cref="User"/> type argument is unused by the algorithm.
/// </remarks>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(user: null!, password);

    public PasswordCheck Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(user: null!, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            _ => PasswordCheck.Failed,
        };
}
