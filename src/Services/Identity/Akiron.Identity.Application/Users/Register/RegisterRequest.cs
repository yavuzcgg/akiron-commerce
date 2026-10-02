using Akiron.Identity.Domain.Common;
using Akiron.Identity.Domain.Users;
using FluentValidation;

namespace Akiron.Identity.Application.Users.Register;

public sealed record RegisterRequest(string? Email, string? Password, string? DisplayName);

/// <summary>
/// Length is the whole password policy (NIST SP 800-63B): composition rules push people
/// towards "Password1!", and length is what actually costs an attacker.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int MinPasswordLength = 8;

    /// <summary>
    /// An upper bound exists only so a megabyte-long "password" cannot make the server
    /// run 100k PBKDF2 iterations over it — a cheap denial of service otherwise.
    /// </summary>
    public const int MaxPasswordLength = 128;

    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(Email.MaxLength);

        RuleFor(request => request.Password)
            .NotEmpty()
            .Length(MinPasswordLength, MaxPasswordLength)
            .WithErrorCode(IdentityErrorCodes.PasswordInvalidLength);

        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MaximumLength(User.MaxDisplayNameLength);
    }
}
