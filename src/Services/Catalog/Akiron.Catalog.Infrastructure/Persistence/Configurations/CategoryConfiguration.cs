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
            .IsRequired();

        builder.Property(category => category.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        // The uniqueness guarantee lives here, not in the handler's pre-check:
        // only the database sees both sides of a race.
        builder.HasIndex(category => category.Slug).IsUnique();
    }
}
