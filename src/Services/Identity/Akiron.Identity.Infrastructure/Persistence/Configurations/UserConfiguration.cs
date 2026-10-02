using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Akiron.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// PasswordHasher v3 output is about 84 characters; the headroom is for a future
    /// format, which must not need a migration just to fit.
    /// </summary>
    private const int MaxPasswordHashLength = 512;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasMaxLength(User.MaxDisplayNameLength)
            .UseCollation("tr-TR-x-icu")
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(MaxPasswordHashLength)
            .IsRequired();

        // The only thing that makes one email one account under concurrency. Emails are
        // stored already lower-cased by the value object, so a plain index is enough.
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
