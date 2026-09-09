using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akiron.Catalog.Infrastructure.Persistence.Configurations;

public sealed class PriceListEntryConfiguration : IEntityTypeConfiguration<PriceListEntry>
{
    public void Configure(EntityTypeBuilder<PriceListEntry> builder)
    {
        // The pair is the key. A group cannot hold two prices for one product, and making
        // that the primary key means the database enforces it without a second unique
        // index to keep in step.
        builder.HasKey(entry => new { entry.PriceGroupId, entry.ProductId });

        builder.ComplexProperty(entry => entry.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, Money.DecimalPlaces);

            price.Property(money => money.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3);
        });

        // Deleting a price group is refused while it still has prices: wiping a list of
        // agreed dealer prices should be a deliberate act, not a side effect.
        builder.HasOne<PriceGroup>()
            .WithMany()
            .HasForeignKey(entry => entry.PriceGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a product does take its prices with it: a price for a product that no
        // longer exists has nothing left to mean.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(entry => entry.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
