using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.ListPriceGroups;

public sealed class ListPriceGroupsHandler(ICatalogDbContext dbContext)
{
    public async Task<PagedResponse<PriceGroupResponse>> HandleAsync(
        ListPriceGroupsRequest request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PriceGroups.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var priceGroups = await Sort(query, request)
            .Skip((request.ResolvedPage - 1) * request.ResolvedPageSize)
            .Take(request.ResolvedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<PriceGroupResponse>(
            [.. priceGroups.Select(PriceGroupResponse.From)],
            request.ResolvedPage,
            request.ResolvedPageSize,
            totalCount);
    }

    private static IQueryable<PriceGroup> Sort(IQueryable<PriceGroup> query, ListPriceGroupsRequest request) =>
        (request.Sort?.ToLowerInvariant(), request.IsDescending) switch
        {
            ("code", false) => query.OrderBy(priceGroup => priceGroup.Code),
            ("code", true) => query.OrderByDescending(priceGroup => priceGroup.Code),
            ("name", false) => query.OrderBy(priceGroup => priceGroup.Name),
            ("name", true) => query.OrderByDescending(priceGroup => priceGroup.Name),
            ("createdat", true) => query.OrderByDescending(priceGroup => priceGroup.CreatedAt),
            ("createdat", false) => query.OrderBy(priceGroup => priceGroup.CreatedAt),
            _ => query.OrderByDescending(priceGroup => priceGroup.CreatedAt),
        };
}
