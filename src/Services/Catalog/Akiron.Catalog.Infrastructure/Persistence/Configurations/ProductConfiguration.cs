using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akiron.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Sku)
            .HasMaxLength(Sku.MaxLength)
            .IsRequired();

        builder.Property(product => product.Name)
            .HasMaxLength(Product.MaxNameLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(Product.MaxDescriptionLength);

        // A complex type, not an owned entity: Money has no identity of its own, and
        // this maps it straight onto two columns of the products table.
        builder.ComplexProperty(product => product.BasePrice, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("base_price_amount")
                .HasPrecision(18, Money.DecimalPlaces);

            price.Property(money => money.Currency)
                .HasColumnName("base_price_currency")
                .HasMaxLength(3);
        });

        // Only the index makes SKUs unique; the handler's pre-check is a courtesy.
        builder.HasIndex(product => product.Sku).IsUnique();

        builder.HasIndex(product => product.CategoryId);

        // Restrict, not cascade: deleting a category must not quietly delete the
        // products in it. The database refuses, and the API answers 409.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
