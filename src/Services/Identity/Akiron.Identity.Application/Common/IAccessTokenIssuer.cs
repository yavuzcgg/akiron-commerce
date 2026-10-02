using Akiron.Identity.Domain.Users;

namespace Akiron.Identity.Application.Common;

/// <summary>Mints the signed access token a user carries to every other service.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}

/// <param name="Value">The compact JWT.</param>
/// <param name="ExpiresAt">When every service will start refusing it.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
