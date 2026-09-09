using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akiron.Catalog.Infrastructure.Persistence.Configurations;

public sealed class PriceGroupConfiguration : IEntityTypeConfiguration<PriceGroup>
{
    public void Configure(EntityTypeBuilder<PriceGroup> builder)
    {
        builder.HasKey(priceGroup => priceGroup.Id);

        builder.Property(priceGroup => priceGroup.Code)
            .HasMaxLength(PriceGroupCode.MaxLength)
            .IsRequired();

        builder.Property(priceGroup => priceGroup.Name)
            .HasMaxLength(PriceGroup.MaxNameLength)
            // Turkish sort order, same reasoning as the category and product names.
            .UseCollation("tr-TR-x-icu")
            .IsRequired();

        builder.Property(priceGroup => priceGroup.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(priceGroup => priceGroup.Discount)
            .HasColumnName("discount_percentage")
            .HasPrecision(5, DiscountPercentage.DecimalPlaces)
            .IsRequired();

        builder.HasIndex(priceGroup => priceGroup.Code).IsUnique();
    }
}
