namespace Akiron.Identity.Application.Common;

/// <summary>
/// Turns passwords into stored hashes and checks them later.
/// </summary>
/// <remarks>
/// A named capability, not a repository: the handlers need exactly these two
/// operations, and the implementation is ASP.NET Core Identity's hasher (ADR-0019).
/// </remarks>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheck Verify(string passwordHash, string password);
}

public enum PasswordCheck
{
    Failed = 1,
    Success = 2,

    /// <summary>Correct, but hashed at an older work factor; store a fresh hash.</summary>
    SuccessRehashNeeded = 3,
}
