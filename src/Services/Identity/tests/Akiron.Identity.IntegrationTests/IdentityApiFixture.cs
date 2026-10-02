using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Identity.Application.Users.Login;
using Akiron.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Akiron.Identity.IntegrationTests;

/// <summary>
/// Boots the real Identity host against a throwaway database and a throwaway signing key.
/// </summary>
/// <remarks>Same shape as Catalog's fixture: one container per collection, Respawn between tests.</remarks>
public sealed class IdentityApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private IdentityApiFactory? _factory;
    private NpgsqlConnection? _connection;
    private Respawner? _respawner;

    /// <summary>Where the host wrote its private key; tests read it to mint tokens on purpose.</summary>
    public string SigningKeyPath { get; } =
        Path.Combine(Path.GetTempPath(), "akiron-identity-tests", $"{Guid.CreateVersion7():N}.pem");

    private IdentityApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("Fixture is not initialised.");

    public JsonSerializerOptions Json =>
        Factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

    public IServiceProvider Services => Factory.Services;

    public HttpClient CreateClient() => Factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        var connectionString = $"{_postgres.GetConnectionString()};Include Error Detail=true";
        _factory = new IdentityApiFactory(connectionString, SigningKeyPath);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>().Database.MigrateAsync();
        }

        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__ef_migrations_history")],
        });
    }

    public Task ResetAsync() =>
        _respawner is null || _connection is null ? Task.CompletedTask : _respawner.ResetAsync(_connection);

    /// <summary>Registers an account and logs in, returning the access token.</summary>
    public async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string password = "correct horse battery")
    {
        var registered = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { email, password, displayName = "Test Bayi" },
            TestContext.Current.CancellationToken);
        registered.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();

        var token = await login.Content.ReadFromJsonAsync<AccessTokenResponse>(Json, TestContext.Current.CancellationToken);
        return token!.AccessToken;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();

        File.Delete(SigningKeyPath);
    }

    private sealed class IdentityApiFactory(string connectionString, string signingKeyPath)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:IdentityDb", connectionString);
            builder.UseSetting("Identity:SigningKeyPath", signingKeyPath);
            builder.UseSetting("Identity:GenerateSigningKeyIfMissing", "true");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class IdentityApiCollectionDefinition : ICollectionFixture<IdentityApiFixture>
{
    public const string Name = "identity-api";
}
