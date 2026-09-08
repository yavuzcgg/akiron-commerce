using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Akiron.Catalog.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only.
/// </summary>
/// <remarks>
/// Without this, the tooling boots the API host to find a context — which means
/// migrations depend on the host's configuration, logging and startup work. This
/// keeps the tooling to a connection string and nothing else.
/// </remarks>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5433;Database=akiron_catalog;Username=akiron;Password=akiron_dev";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDb")
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        CatalogDbContextOptions.Apply(options, connectionString);

        return new CatalogDbContext(options.Options);
    }
}
