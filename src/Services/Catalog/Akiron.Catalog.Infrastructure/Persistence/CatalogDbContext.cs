using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Akiron.Catalog.Infrastructure.Persistence.Configurations;
using Akiron.Catalog.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), ICatalogDbContext
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<PriceGroup> PriceGroups => Set<PriceGroup>();

    public DbSet<PriceListEntry> PriceListEntries => Set<PriceListEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Listed one by one rather than scanned from the assembly: the set of
        // configured entities stays visible in the file you are already reading.
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new PriceGroupConfiguration());
        modelBuilder.ApplyConfiguration(new PriceListEntryConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Registered once per value type here, so no entity configuration has to
        // remember to call HasConversion for an id or a slug.
        configurationBuilder.Properties<CategoryId>().HaveConversion<CategoryIdConverter>();
        configurationBuilder.Properties<Slug>().HaveConversion<SlugConverter>();
        configurationBuilder.Properties<ProductId>().HaveConversion<ProductIdConverter>();
        configurationBuilder.Properties<Sku>().HaveConversion<SkuConverter>();
        configurationBuilder.Properties<Currency>().HaveConversion<CurrencyConverter>();
        configurationBuilder.Properties<PriceGroupId>().HaveConversion<PriceGroupIdConverter>();
        configurationBuilder.Properties<PriceGroupCode>().HaveConversion<PriceGroupCodeConverter>();
        configurationBuilder.Properties<DiscountPercentage>().HaveConversion<DiscountPercentageConverter>();
    }
}
