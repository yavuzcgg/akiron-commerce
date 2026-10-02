using Akiron.Identity.Domain.Users;

namespace Akiron.Identity.Application.Users;

/// <summary>A user as the API shows it. The password hash never leaves the service.</summary>
public sealed record UserResponse(UserId Id, string Email, string DisplayName, DateTimeOffset CreatedAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Email.Value, user.DisplayName, user.CreatedAt);
}
