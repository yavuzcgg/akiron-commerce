using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akiron.Catalog.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(Category.MaxNameLength)
            // Sorted with Turkish rules. Under the database default (en_US) the Turkish
            // letters land after z, so a category list would show Celik Jant below
            // Zincir. Measured before choosing this: en_US gives
            // istanbul < zula < cakmak, tr-TR-x-icu gives cakmak < isi < istanbul < zula.
            .UseCollation("tr-TR-x-icu")
            .IsRequired();

        builder.Property(category => category.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        // The uniqueness guarantee lives here, not in the handler's pre-check:
        // only the database sees both sides of a race.
        builder.HasIndex(category => category.Slug).IsUnique();
    }
}
