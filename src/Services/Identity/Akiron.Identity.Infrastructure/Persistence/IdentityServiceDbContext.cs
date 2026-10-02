using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Users;
using Akiron.Identity.Infrastructure.Persistence.Configurations;
using Akiron.Identity.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Identity.Infrastructure.Persistence;

/// <summary>
/// The Identity service's database.
/// </summary>
/// <remarks>
/// Not called IdentityDbContext on purpose: that is ASP.NET Core Identity's base class,
/// which this service deliberately does not use (ADR-0019), and the name would suggest
/// otherwise to anyone reading it.
/// </remarks>
public sealed class IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
    : DbContext(options), IIdentityDbContext
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter>();
        configurationBuilder.Properties<Email>().HaveConversion<EmailConverter>();
    }
}
