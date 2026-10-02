using Akiron.Identity.Domain.Common;
using Akiron.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Akiron.Identity.Api.Common;

/// <summary>
/// Validates bearer tokens with the <em>public</em> key — exactly what Catalog and the
/// other services will do in 2.4, here inside the service that issued them.
/// </summary>
public static class BearerAuthentication
{
    public static IServiceCollection AddBearerAuthentication(
        this IServiceCollection services,
        SigningKeyStore keyStore,
        TokenSettings tokenSettings)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep the JWT claim names (sub, email). The default rewrites them into
                // long WS-Federation URIs, and code then has to know both spellings.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = tokenSettings.Issuer,
                    ValidAudience = tokenSettings.Audience,
                    IssuerSigningKey = keyStore.VerificationKey,

                    // Exactly one algorithm, so the token's own header never gets a say in
                    // how it is checked. Measured: this is NOT what stops "alg: none" —
                    // RequireSignedTokens (true by default) does that, and removing only this
                    // line left that test green. It is the second lock against algorithm
                    // confusion, should a key of another type ever be configured.
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],

                    // The default tolerance is five minutes, a third of the token's life.
                    // Containers on one host do not drift that far.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents { OnChallenge = WriteProblemAsync };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// A 401 with the same RFC 7807 body and error code as every other failure (ADR-0018),
    /// instead of the empty response the handler writes by default.
    /// </summary>
    private static async Task WriteProblemAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "A valid bearer token is required.",
        };

        problemDetails.Extensions["code"] = IdentityErrorCodes.Unauthenticated;

        await context.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .TryWriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problemDetails });
    }
}
