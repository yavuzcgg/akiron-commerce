using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Identity.Application.Users.Login;
using Akiron.Identity.Domain.Common;
using AwesomeAssertions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Akiron.Identity.IntegrationTests;

[Collection(IdentityApiCollectionDefinition.Name)]
public sealed class LoginTests(IdentityApiFixture fixture) : IAsyncLifetime
{
    private const string Password = "correct horse battery";

    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Login_ReturnsAFifteenMinuteBearerToken_AboutTheRightUser()
    {
        var client = fixture.CreateClient();
        await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev", Password);

        var response = await LoginAsync(client, "BAYI@akiron.dev", Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the email is matched case-insensitively");

        var token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(fixture.Json, TestContext.Current.CancellationToken);
        token!.TokenType.Should().Be("Bearer");
        token.ExpiresIn.Should().BeInRange(899, 900);

        var jwt = new JsonWebToken(token.AccessToken);
        jwt.Alg.Should().Be("RS256");
        jwt.Issuer.Should().Be("akiron-identity");
        jwt.Audiences.Should().Equal("akiron");
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.Should().Be("bayi@akiron.dev");
        (jwt.ValidTo - jwt.IssuedAt).Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task AWrongPasswordAndAnUnknownEmail_LookExactlyTheSame()
    {
        // If these differed in status, code or wording, the login form would answer
        // "does this person have an account?" for anyone who asked.
        var client = fixture.CreateClient();
        await fixture.RegisterAndLoginAsync(client, "bayi@akiron.dev", Password);

        var wrongPassword = await LoginAsync(client, "bayi@akiron.dev", "wrong password!");
        var unknownEmail = await LoginAsync(client, "nobody@akiron.dev", "wrong password!");

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var first = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var second = await unknownEmail.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        first.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.InvalidCredentials);
        second.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.InvalidCredentials);
        first.GetProperty("detail").GetString().Should().Be(second.GetProperty("detail").GetString());
    }
}
