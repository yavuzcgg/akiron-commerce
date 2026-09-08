using System.Text.Json;
using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Akiron.Catalog.IntegrationTests;

/// <summary>
/// One PostgreSQL container for the whole collection, migrated once and emptied
/// between tests.
/// </summary>
/// <remarks>
/// A container per test would be correct and unusably slow; sharing one without
/// resetting would make tests depend on each other's leftovers. Respawn deletes the
/// rows between tests, which keeps the schema (and the migration cost) intact.
/// </remarks>
public sealed class CatalogApiFixture : IAsyncLifetime
{
    /// <summary>Matches the image the local compose stack runs, so tests and dev see the same server.</summary>
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private CatalogApiFactory? _factory;
    private NpgsqlConnection? _connection;
    private Respawner? _respawner;

    /// <summary>Serializer settings that mirror the API's, typed ids included.</summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new CategoryIdJsonConverter() },
    };

    public HttpClient CreateClient() =>
        (_factory ?? throw new InvalidOperationException("Fixture is not initialised.")).CreateClient();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Include Error Detail makes Npgsql report the violated constraint name. The
        // API's conflict mapping keys on that name, so without this the duplicate-slug
        // test would pass for the wrong reason.
        var connectionString = $"{_postgres.GetConnectionString()};Include Error Detail=true";

        _factory = new CatalogApiFactory(connectionString);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            // The real migrations, not EnsureCreated: this is also the only automated
            // check that they actually apply to an empty database.
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await dbContext.Database.MigrateAsync();
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
        _respawner is null || _connection is null
            ? Task.CompletedTask
            : _respawner.ResetAsync(_connection);

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
    }
}

[CollectionDefinition(Name)]
public sealed class CatalogApiCollectionDefinition : ICollectionFixture<CatalogApiFixture>
{
    public const string Name = "catalog-api";
}
