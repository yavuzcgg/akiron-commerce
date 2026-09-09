using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(ICatalogDbContext dbContext)
{
    public async Task<CategoryResponse> HandleAsync(
        CategoryId categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(candidate => candidate.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException($"Category '{categoryId}' was not found.");

        category.Rename(request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CategoryResponse.From(category);
    }
}
