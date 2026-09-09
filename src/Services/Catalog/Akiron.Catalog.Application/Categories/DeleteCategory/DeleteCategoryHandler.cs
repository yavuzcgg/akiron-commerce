using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(ICatalogDbContext dbContext)
{
    public async Task HandleAsync(CategoryId categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(candidate => candidate.Id == categoryId, cancellationToken)
            ?? throw new NotFoundException($"Category '{categoryId}' was not found.");

        // No check for products here on purpose: the foreign key is configured to
        // restrict, so the database refuses the delete and CatalogExceptionHandler maps
        // that to a 409. Checking first would only add a race the constraint already wins.
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
