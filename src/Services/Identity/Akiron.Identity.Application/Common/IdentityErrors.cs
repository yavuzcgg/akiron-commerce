using Akiron.Identity.Domain.Common;
using Akiron.Identity.Domain.Users;

namespace Akiron.Identity.Application.Common;

/// <summary>Every failure a handler can raise, built in one place.</summary>
public static class IdentityErrors
{
    public static ConflictException EmailTaken() => new(
        IdentityErrorCodes.EmailConflict,
        "An account with this email already exists.");

    public static NotFoundException UserNotFound(UserId userId) => new(
        IdentityErrorCodes.UserNotFound,
        $"User '{userId}' was not found.",
        new Dictionary<string, object?> { ["userId"] = userId.Value });

    /// <summary>
    /// One message for "no such email" and "wrong password" alike. Telling them apart
    /// would turn the login form into a lookup service for who has an account.
    /// </summary>
    public static InvalidCredentialsException InvalidCredentials() => new(
        IdentityErrorCodes.InvalidCredentials,
        "The email or password is incorrect.");

    public static ConflictException UnexpectedConflict() => new(
        IdentityErrorCodes.Conflict,
        "The request conflicts with the current state of the data.");
}
