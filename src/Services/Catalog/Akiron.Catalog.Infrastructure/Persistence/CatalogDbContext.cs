using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Infrastructure.Persistence.Configurations;
using Akiron.Catalog.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), ICatalogDbContext
{
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Listed one by one rather than scanned from the assembly: the set of
        // configured entities stays visible in the file you are already reading.
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Registered once per value type here, so no entity configuration has to
        // remember to call HasConversion for an id or a slug.
        configurationBuilder.Properties<CategoryId>().HaveConversion<CategoryIdConverter>();
        configurationBuilder.Properties<Slug>().HaveConversion<SlugConverter>();
    }
}
