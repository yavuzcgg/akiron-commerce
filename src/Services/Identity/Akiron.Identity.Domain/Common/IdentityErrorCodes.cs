namespace Akiron.Identity.Domain.Common;

/// <summary>
/// Every failure this service can report, named once.
/// </summary>
/// <remarks>
/// Part of the public API contract (ADR-0018): clients branch on and translate these,
/// so renaming one is a breaking change. Naming is service.resource.reason; the two
/// cross-cutting codes are shared with Catalog on purpose, so a client handles them once.
/// </remarks>
public static class IdentityErrorCodes
{
    // Domain guards.
    public const string TextRequired = "identity.text.required";
    public const string TextTooLong = "identity.text.too_long";
    public const string EmailInvalidFormat = "identity.email.invalid_format";
    public const string EmailTooLong = "identity.email.too_long";
    public const string PasswordInvalidLength = "identity.password.invalid_length";

    // Outcomes a caller can act on.
    public const string EmailConflict = "identity.user.email_conflict";
    public const string UserNotFound = "identity.user.not_found";
    public const string InvalidCredentials = "identity.auth.invalid_credentials";
    public const string Unauthenticated = "identity.auth.unauthenticated";

    // Cross-cutting.
    public const string ValidationFailed = "validation_failed";
    public const string Conflict = "identity.conflict";
    public const string Unexpected = "unexpected_error";
}
