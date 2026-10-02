using Akiron.Identity.Application.Users.Register;
using FluentValidation;

namespace Akiron.Identity.Application.Users.Login;

public sealed record LoginRequest(string? Email, string? Password);

/// <param name="ExpiresIn">Seconds until the token expires, as OAuth 2.0 responses spell it.</param>
public sealed record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn);

/// <summary>
/// Presence only — no minimum length here. Login must not reveal the password policy,
/// and an account created under an older, looser policy must still be able to sign in.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(RegisterRequestValidator.MaxPasswordLength);
    }
}
