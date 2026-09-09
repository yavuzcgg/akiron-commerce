using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.ListPrices;

public sealed class ListPricesHandler(ICatalogDbContext dbContext)
{
    public async Task<PagedResponse<PriceListEntryResponse>> HandleAsync(
        string? rawCode,
        ListPricesRequest request,
        CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(rawCode);

        var priceGroup = await dbContext.PriceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        var query = dbContext.PriceListEntries
            .AsNoTracking()
            .Where(entry => entry.PriceGroupId == priceGroup.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var entries = await Sort(query, request)
            .Skip((request.ResolvedPage - 1) * request.ResolvedPageSize)
            .Take(request.ResolvedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<PriceListEntryResponse>(
            [.. entries.Select(PriceListEntryResponse.From)],
            request.ResolvedPage,
            request.ResolvedPageSize,
            totalCount);
    }

    private static IQueryable<PriceListEntry> Sort(IQueryable<PriceListEntry> query, ListPricesRequest request) =>
        (request.Sort?.ToLowerInvariant(), request.IsDescending) switch
        {
            ("amount", false) => query.OrderBy(entry => entry.Price.Amount),
            ("amount", true) => query.OrderByDescending(entry => entry.Price.Amount),
            ("createdat", true) => query.OrderByDescending(entry => entry.CreatedAt),
            ("createdat", false) => query.OrderBy(entry => entry.CreatedAt),
            _ => query.OrderByDescending(entry => entry.CreatedAt),
        };
}
