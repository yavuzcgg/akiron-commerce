using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Akiron.Identity.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only, so migrations need a connection string and nothing else.</summary>
public sealed class IdentityServiceDbContextFactory : IDesignTimeDbContextFactory<IdentityServiceDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5433;Database=akiron_identity;Username=akiron;Password=akiron_dev";

    public IdentityServiceDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__IdentityDb")
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<IdentityServiceDbContext>();
        IdentityServiceDbContextOptions.Apply(options, connectionString);

        return new IdentityServiceDbContext(options.Options);
    }
}
