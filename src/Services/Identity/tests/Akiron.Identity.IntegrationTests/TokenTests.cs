using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Akiron.Identity.Domain.Common;
using AwesomeAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Akiron.Identity.IntegrationTests;

/// <summary>
/// The point of asymmetric signing, tested from the outside: anyone can verify a token
/// with the published key, and nobody without the private key can make one that passes.
/// </summary>
[Collection(IdentityApiCollectionDefinition.Name)]
public sealed class TokenTests(IdentityApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Signs arbitrary claims with the given key, the way an attacker — or Identity — would.</summary>
    private static string Mint(RSA rsa, string? keyId, DateTime expires, IDictionary<string, object> claims) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "akiron-identity",
            Audience = "akiron",
            IssuedAt = expires.AddMinutes(-15),
            NotBefore = expires.AddMinutes(-15),
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = keyId }, SecurityAlgorithms.RsaSha256),
        });

    private static string Base64Url(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));

    [Fact]
    public async Task Jwks_PublishesThePublicKeyOnly()
    {
        var jwks = await fixture.CreateClient().GetFromJsonAsync<JsonElement>(
            "/.well-known/jwks.json", TestContext.Current.CancellationToken);

        var key = jwks.GetProperty("keys").EnumerateArray().Should().ContainSingle().Subject;
        key.GetProperty("kty").GetString().Should().Be("RSA");
        key.GetProperty("alg").GetString().Should().Be("RS256");

        // d, p, q, dp, dq, qi are the private parts of an RSA JWK.
        foreach (var privatePart in new[] { "d", "p", "q", "dp", "dq", "qi" })
        {
            key.TryGetProperty(privatePart, out _).Should().BeFalse($"'{privatePart}' would leak the private key");
        }
    }

    [Fact]
    public async Task AnotherServiceCanVerifyATokenUsingNothingButTheJwks()
    {
        // What Catalog will do in 2.4, done by hand: fetch the JWKS, rebuild the key from
        // n and e, validate. No call to Identity, no shared secret.
        var client = fixture.CreateClient();
        var token = await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev");
        var jwks = await client.GetStringAsync(new Uri("/.well-known/jwks.json", UriKind.Relative), TestContext.Current.CancellationToken);

        var keys = new JsonWebKeySet(jwks).GetSigningKeys();

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "akiron-identity",
            ValidAudience = "akiron",
            IssuerSigningKeys = keys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        });

        result.IsValid.Should().BeTrue(result.Exception?.Message);
        new JsonWebToken(token).Kid.Should().Be(keys.Single().KeyId, "the token's kid tells a verifier which key to use");
    }

    [Fact]
    public async Task Me_WithAValidToken_ReturnsTheCaller()
    {
        var client = fixture.CreateClient();
        var token = await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev");

        var response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("email").GetString().Should().Be("bayi@akiron.dev");
    }

    [Fact]
    public async Task Me_WithoutAToken_IsUnauthorized_WithAnErrorCode()
    {
        var response = await GetMeAsync(fixture.CreateClient(), token: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle(header => header.Scheme == "Bearer");
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("code").GetString().Should().Be(IdentityErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task ATokenSignedWithSomeoneElsesKey_IsRejected()
    {
        // Correct issuer, audience, claims and algorithm — everything but the key.
        var client = fixture.CreateClient();
        var genuine = new JsonWebToken(await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev"));

        using var attackerKey = RSA.Create(2048);
        var forged = Mint(attackerKey, genuine.Kid, DateTime.UtcNow.AddMinutes(10), new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = genuine.Subject,
        });

        (await GetMeAsync(client, forged)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ATokenWhosePayloadWasEdited_IsRejected()
    {
        // Swap the payload for one naming another user, keep the original signature.
        var client = fixture.CreateClient();
        var token = await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev");
        var parts = token.Split('.');

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64UrlEncoder.Decode(parts[1]))!;
        var edited = payload.ToDictionary(
            claim => claim.Key,
            claim => claim.Key == "sub" ? (object)Guid.CreateVersion7().ToString() : claim.Value);

        var tampered = $"{parts[0]}.{Base64Url(JsonSerializer.Serialize(edited))}.{parts[2]}";

        (await GetMeAsync(client, tampered)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnUnsignedAlgNoneToken_IsRejected()
    {
        // The classic: declare "alg": "none", drop the signature, hope the validator
        // believes the header. Refused by RequireSignedTokens, which defaults to true —
        // this test is what notices if someone ever turns it off.
        var client = fixture.CreateClient();
        var genuine = new JsonWebToken(await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev"));

        var header = Base64Url("""{"alg":"none","typ":"JWT"}""");
        var payload = Base64Url(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["sub"] = genuine.Subject,
            ["iss"] = "akiron-identity",
            ["aud"] = "akiron",
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
        }));

        (await GetMeAsync(client, $"{header}.{payload}.")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AGenuineButExpiredToken_IsRejected()
    {
        // Signed with the real key, so only the expiry can fail it. Expired by a minute,
        // which is past the 30-second clock skew allowance.
        var client = fixture.CreateClient();
        var genuine = new JsonWebToken(await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev"));

        using var realKey = RSA.Create();
        realKey.ImportFromPem(await File.ReadAllTextAsync(fixture.SigningKeyPath, TestContext.Current.CancellationToken));

        var expired = Mint(realKey, genuine.Kid, DateTime.UtcNow.AddMinutes(-1), new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = genuine.Subject,
        });

        (await GetMeAsync(client, expired)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
