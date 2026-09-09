using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Categories.ListCategories;

public sealed class ListCategoriesHandler(ICatalogDbContext dbContext)
{
    public async Task<PagedResponse<CategoryResponse>> HandleAsync(
        ListCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Categories.AsNoTracking();

        // Counted before paging, so the client can work out how many pages exist.
        var totalCount = await query.CountAsync(cancellationToken);

        var categories = await Sort(query, request)
            .Skip((request.ResolvedPage - 1) * request.ResolvedPageSize)
            .Take(request.ResolvedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CategoryResponse>(
            [.. categories.Select(CategoryResponse.From)],
            request.ResolvedPage,
            request.ResolvedPageSize,
            totalCount);
    }

    /// <summary>
    /// Sorting is a switch over a fixed set, never a string handed to the database.
    /// The validator has already rejected anything outside that set; this simply maps
    /// the accepted names onto expressions.
    /// </summary>
    private static IQueryable<Category> Sort(IQueryable<Category> query, ListCategoriesRequest request) =>
        (request.Sort?.ToLowerInvariant(), request.IsDescending) switch
        {
            ("name", false) => query.OrderBy(category => category.Name),
            ("name", true) => query.OrderByDescending(category => category.Name),
            ("createdat", true) => query.OrderByDescending(category => category.CreatedAt),
            ("createdat", false) => query.OrderBy(category => category.CreatedAt),

            // Newest first: the default a list of anything editable wants.
            _ => query.OrderByDescending(category => category.CreatedAt),
        };
}
