using Akiron.Identity.Infrastructure.Security;

namespace Akiron.Identity.Api.Keys;

/// <summary>
/// Publishes the public signing key(s) as a JSON Web Key Set (RFC 7517).
/// </summary>
/// <remarks>
/// Every service fetches this once, caches it, and verifies tokens locally — no call to
/// Identity per request (ADR-0011). The document is built field by field rather than by
/// serialising IdentityModel's JsonWebKey: that type also carries private-key fields, and
/// whether they stay empty should be visible in this file, not trusted to a serializer.
/// </remarks>
public static class JwksEndpoint
{
    public static void MapJwksEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/jwks.json", (SigningKeyStore keyStore, HttpContext httpContext) =>
            {
                // Keys change rarely, and rotation publishes the new key before signing with it.
                httpContext.Response.Headers.CacheControl = "public, max-age=300";

                var key = keyStore.PublicJwk;
                return Results.Ok(new JsonWebKeySetDocument([new PublicKey(key.Kty, key.Use, key.Alg, key.Kid, key.N, key.E)]));
            })
            .WithTags("Keys")
            .Produces<JsonWebKeySetDocument>()
            .WithSummary("The public keys that verify this service's tokens.");
    }

    public sealed record JsonWebKeySetDocument(IReadOnlyList<PublicKey> Keys);

    /// <param name="Kty">Key type: RSA.</param>
    /// <param name="Use">sig — for signatures, not encryption.</param>
    /// <param name="Alg">RS256.</param>
    /// <param name="Kid">Matches the kid in each token's header, so a verifier picks the right key.</param>
    /// <param name="N">The RSA modulus, base64url.</param>
    /// <param name="E">The RSA public exponent, base64url.</param>
    public sealed record PublicKey(string Kty, string Use, string Alg, string Kid, string N, string E);
}
