using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Identity.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Identity.IntegrationTests;

[Collection(IdentityApiCollectionDefinition.Name)]
public sealed class RegisterTests(IdentityApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = "correct horse battery") =>
        client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, displayName = "Yavuz Çelik" },
            TestContext.Current.CancellationToken);

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    [Fact]
    public async Task Register_ReturnsTheUser_WithTheEmailNormalised_AndNoPasswordHash()
    {
        var response = await RegisterAsync(fixture.CreateClient(), "  Yavuz@Akiron.DEV ");

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await BodyAsync(response);
        body.GetProperty("email").GetString().Should().Be("yavuz@akiron.dev");
        body.GetProperty("displayName").GetString().Should().Be("Yavuz Çelik");
        body.TryGetProperty("passwordHash", out _).Should().BeFalse("the hash never leaves the service");
    }

    [Fact]
    public async Task Register_TheSameEmailInAnotherCase_IsAConflict()
    {
        var client = fixture.CreateClient();
        await RegisterAsync(client, "yavuz@akiron.dev");

        var response = await RegisterAsync(client, "YAVUZ@akiron.dev");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await BodyAsync(response)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.EmailConflict);
    }

    [Fact]
    public async Task Register_WithAShortPassword_NamesTheRuleThatFailed()
    {
        var response = await RegisterAsync(fixture.CreateClient(), "yavuz@akiron.dev", password: "kisa");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errors = (await BodyAsync(response)).GetProperty("errors").GetProperty("password");
        errors[0].GetProperty("code").GetString().Should().Be(IdentityErrorCodes.PasswordInvalidLength);
    }

    [Fact]
    public async Task Register_WithAMalformedEmail_IsABadRequest()
    {
        var response = await RegisterAsync(fixture.CreateClient(), "not-an-email");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BodyAsync(response)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.EmailInvalidFormat);
    }

    [Fact]
    public async Task TwoRegistrationsRacingForOneEmail_OneWins_AndTheOtherIsAConflictNotACrash()
    {
        // Both requests pass the handler's "is this email free?" check before either has
        // written; only the unique index can pick a winner. Without the index mapping in
        // IdentityExceptionHandler the loser would be a 500.
        var client = fixture.CreateClient();

        for (var round = 0; round < 10; round++)
        {
            var email = $"race-{round}@akiron.dev";
            var responses = await Task.WhenAll(RegisterAsync(client, email), RegisterAsync(client, email));

            responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
                [HttpStatusCode.Created, HttpStatusCode.Conflict],
                $"round {round}: exactly one registration may win");
        }
    }
}
