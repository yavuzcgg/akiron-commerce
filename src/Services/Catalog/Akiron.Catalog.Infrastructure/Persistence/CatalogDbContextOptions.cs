using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Infrastructure.Persistence;

/// <summary>
/// One place to build the context options.
/// </summary>
/// <remarks>
/// The runtime host, the design-time factory and the integration tests all call
/// this. If the snake_case convention were applied in only some of them, migrations
/// would be generated against a different model than the one the app runs on.
/// </remarks>
public static class CatalogDbContextOptions
{
    public static DbContextOptionsBuilder Apply(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}
