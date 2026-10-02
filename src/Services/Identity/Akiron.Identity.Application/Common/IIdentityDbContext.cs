using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Identity.Application.Common;

/// <summary>
/// The Application layer's window onto persistence; it exists only to invert the
/// Application → Infrastructure reference.
/// </summary>
/// <remarks>
/// Same rule as Catalog's: this interface must never gain a method. Handlers write
/// their own LINQ against the sets.
/// </remarks>
public interface IIdentityDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
