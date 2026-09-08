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
            ?? throw new NotFoundException($"Category '{categoryId}' was not found.");

        return CategoryResponse.From(category);
    }
}
