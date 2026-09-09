using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Application.Common;

/// <summary>
/// Sets the agreed price of a product in a group, creating the row or replacing it in a
/// single atomic step.
/// </summary>
/// <remarks>
/// A named capability, not a repository: it exists because this one operation cannot be
/// expressed through <see cref="ICatalogDbContext"/> without a read-then-write gap that
/// two concurrent callers can both fall into. Everything else in the Application layer
/// still writes its own LINQ against the sets.
/// </remarks>
public interface IPriceListWriter
{
    Task<PriceUpsertOutcome> UpsertAsync(
        PriceGroupId priceGroupId,
        ProductId productId,
        Money price,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);
}

/// <summary>
/// What the upsert did. <c>Inserted</c> is what lets the endpoint answer 201 the first
/// time and 200 afterwards.
/// </summary>
public readonly record struct PriceUpsertOutcome(
    bool Inserted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
