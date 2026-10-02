using Microsoft.EntityFrameworkCore;

namespace Akiron.Identity.Infrastructure.Persistence;

/// <summary>
/// One place to build the context options, shared by the host, the design-time factory
/// and the tests — so migrations are generated against the model the app runs on.
/// </summary>
public static class IdentityServiceDbContextOptions
{
    public static DbContextOptionsBuilder Apply(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}
