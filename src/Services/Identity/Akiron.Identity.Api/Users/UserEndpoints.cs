using System.Security.Claims;
using Akiron.Identity.Api.Common;
using Akiron.Identity.Application.Users;
using Akiron.Identity.Application.Users.GetCurrentUser;
using Akiron.Identity.Application.Users.Login;
using Akiron.Identity.Application.Users.Register;
using Akiron.Identity.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Akiron.Identity.Api.Users;

public static class UserEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // No Location header: there is no public "get user by id" route to point at, and
        // inventing one only to satisfy the convention would expose every account by id.
        group.MapPost("/register", async (
                RegisterRequest request,
                RegisterHandler handler,
                CancellationToken cancellationToken) =>
                Results.Created((string?)null, await handler.HandleAsync(request, cancellationToken)))
            .Validate<RegisterRequest>()
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Creates an account. Does not log in — call /login for a token.");

        group.MapPost("/login", async (
                LoginRequest request,
                LoginHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request, cancellationToken)))
            .Validate<LoginRequest>()
            .Produces<AccessTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Exchanges an email and password for a 15-minute access token.");
    }

    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/users/me", async (
                ClaimsPrincipal principal,
                GetCurrentUserHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(ReadUserId(principal), cancellationToken)))
            .RequireAuthorization()
            .WithTags("Users")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Returns the account the bearer token belongs to.");
    }

    /// <summary>
    /// The token was already verified by the time this runs, and Identity always writes a
    /// uuid sub — so a missing or malformed one is a bug, not bad input.
    /// </summary>
    private static UserId ReadUserId(ClaimsPrincipal principal) =>
        UserId.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), provider: null, out var userId)
            ? userId
            : throw new InvalidOperationException("A verified token carried no usable 'sub' claim.");
}
