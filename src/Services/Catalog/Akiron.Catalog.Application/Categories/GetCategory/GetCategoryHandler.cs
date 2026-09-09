using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Categories.GetCategory;

public sealed class GetCategoryHandler(ICatalogDbContext dbContext)
{
    public async Task<CategoryResponse> HandleAsync(CategoryId categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == categoryId, cancellationToken)
            ?? throw CatalogErrors.CategoryNotFound(categoryId);

        return CategoryResponse.From(category);
    }
}
