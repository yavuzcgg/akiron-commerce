using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Products.ListProducts;

public sealed class ListProductsHandler(ICatalogDbContext dbContext)
{
    public async Task<PagedResponse<ProductResponse>> HandleAsync(
        ListProductsRequest request,
        CancellationToken cancellationToken)
    {
        var query = Filter(dbContext.Products.AsNoTracking(), request);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await Sort(query, request)
            .Skip((request.ResolvedPage - 1) * request.ResolvedPageSize)
            .Take(request.ResolvedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductResponse>(
            [.. products.Select(ProductResponse.From)],
            request.ResolvedPage,
            request.ResolvedPageSize,
            totalCount);
    }

    private static IQueryable<Product> Filter(IQueryable<Product> query, ListProductsRequest request)
    {
        if (request.CategoryId is { } categoryId)
        {
            var typedId = new CategoryId(categoryId);
            query = query.Where(product => product.CategoryId == typedId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(product => product.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            // Through the value object, so a lower-case query finds the stored upper-case
            // row. Anything malformed throws here and comes back as a 400, which is a
            // truthful answer: that SKU could never exist.
            var sku = Sku.Create(request.Sku);
            query = query.Where(product => product.Sku == sku);
        }

        return query;
    }

    private static IQueryable<Product> Sort(IQueryable<Product> query, ListProductsRequest request) =>
        (request.Sort?.ToLowerInvariant(), request.IsDescending) switch
        {
            ("name", false) => query.OrderBy(product => product.Name),
            ("name", true) => query.OrderByDescending(product => product.Name),
            ("baseprice", false) => query.OrderBy(product => product.BasePrice.Amount),
            ("baseprice", true) => query.OrderByDescending(product => product.BasePrice.Amount),
            ("createdat", true) => query.OrderByDescending(product => product.CreatedAt),
            ("createdat", false) => query.OrderBy(product => product.CreatedAt),
            _ => query.OrderByDescending(product => product.CreatedAt),
        };
}
