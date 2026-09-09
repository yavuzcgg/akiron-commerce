using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
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

        var responses = await WithGroupPricesAsync(products, request.PriceGroup, cancellationToken);

        return new PagedResponse<ProductResponse>(
            responses, request.ResolvedPage, request.ResolvedPageSize, totalCount);
    }

    /// <summary>
    /// Resolves what one price group pays for the products on this page.
    /// </summary>
    /// <remarks>
    /// Two queries for the page rather than one per row: the storefront renders a grid of
    /// these, and a query per product is how a listing quietly becomes slow.
    /// </remarks>
    private async Task<IReadOnlyList<ProductResponse>> WithGroupPricesAsync(
        List<Product> products,
        string? rawPriceGroupCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawPriceGroupCode))
        {
            return [.. products.Select(ProductResponse.From)];
        }

        var code = PriceGroupCode.Create(rawPriceGroupCode);

        var priceGroup = await dbContext.PriceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        var productIds = products.Select(product => product.Id).ToArray();

        var agreedPrices = await dbContext.PriceListEntries
            .AsNoTracking()
            .Where(entry => entry.PriceGroupId == priceGroup.Id && productIds.Contains(entry.ProductId))
            .ToDictionaryAsync(entry => entry.ProductId, entry => entry.Price, cancellationToken);

        using var activity = CatalogActivitySource.Instance.StartActivity(CatalogActivitySource.ResolvePrices);
        activity?.SetTag("catalog.price_group", code.Value);
        activity?.SetTag("catalog.markup_depth", 0);
        activity?.SetTag("catalog.product_count", products.Count);

        return
        [
            .. products.Select(product =>
            {
                agreedPrices.TryGetValue(product.Id, out var agreedPrice);

                // No markup chain here: a listing shows what this group pays, and the
                // sub-dealer chain belongs to a buyer, which the catalogue does not know
                // about until Identity arrives.
                var quote = PriceResolver.Resolve(
                    product.BasePrice, agreedPrice, priceGroup.Discount, MarkupChain.Empty);

                return ProductResponse.From(product).WithGroupPrice(quote.FinalPrice);
            }),
        ];
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
