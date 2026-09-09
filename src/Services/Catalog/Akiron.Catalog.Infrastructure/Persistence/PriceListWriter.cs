using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Infrastructure.Persistence;

/// <summary>
/// Writes a price with PostgreSQL's own upsert.
/// </summary>
/// <remarks>
/// The one place in this service that sends SQL rather than LINQ, because EF Core has no
/// way to express INSERT ... ON CONFLICT and the alternative — check, then insert — has a
/// gap between the two steps that concurrent callers fall into.
/// </remarks>
public sealed class PriceListWriter(CatalogDbContext dbContext) : IPriceListWriter
{
    public async Task<PriceUpsertOutcome> UpsertAsync(
        PriceGroupId priceGroupId,
        ProductId productId,
        Money price,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var currency = price.Currency.ToString();

        // xmax is the transaction that deleted or superseded a row; on a freshly inserted
        // row it is 0, and on one that ON CONFLICT updated it is the current transaction.
        // Comparing it to 0 is how PostgreSQL tells you which branch ran, without a second
        // query.
        var rows = await dbContext.Database
            .SqlQuery<UpsertRow>($"""
                INSERT INTO price_list_entries (price_group_id, product_id, amount, currency, created_at, updated_at)
                VALUES ({priceGroupId.Value}, {productId.Value}, {price.Amount}, {currency}, {timestamp}, NULL)
                ON CONFLICT (price_group_id, product_id) DO UPDATE
                    SET amount = EXCLUDED.amount,
                        currency = EXCLUDED.currency,
                        updated_at = {timestamp}
                RETURNING (xmax = 0) AS inserted, created_at, updated_at
                """)
            .ToListAsync(cancellationToken);

        var row = rows[0];

        return new PriceUpsertOutcome(row.Inserted, row.CreatedAt, row.UpdatedAt);
    }

    /// <summary>
    /// Shape of the RETURNING clause.
    /// </summary>
    /// <remarks>
    /// The snake_case naming convention applies to raw SQL results too, so the columns
    /// have to come back as inserted, created_at and updated_at rather than as the
    /// property names — EF looks for the converted form.
    /// </remarks>
    private sealed class UpsertRow
    {
        public bool Inserted { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
