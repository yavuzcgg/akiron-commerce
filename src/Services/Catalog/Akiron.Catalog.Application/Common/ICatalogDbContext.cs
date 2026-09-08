using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Common;

/// <summary>
/// The Application layer's window onto persistence. It exists for exactly one
/// reason: to invert the Application → Infrastructure reference so the layer rule
/// in AGENTS.md holds.
/// </summary>
/// <remarks>
/// This interface must never gain a method. Adding <c>GetById</c>, <c>FindAsync</c>
/// or any other query helper turns it into the generic repository AGENTS.md
/// forbids — handlers are expected to write their own LINQ against the sets.
/// </remarks>
public interface ICatalogDbContext
{
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
