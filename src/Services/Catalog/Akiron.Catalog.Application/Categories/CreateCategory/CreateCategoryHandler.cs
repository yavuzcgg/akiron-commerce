using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Categories.CreateCategory;

public sealed class CreateCategoryHandler(ICatalogDbContext dbContext)
{
    public async Task<CategoryResponse> HandleAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var slug = Slug.Create(request.Slug);

        // This lookup buys a clean 409 for the ordinary case. It does NOT make the
        // slug unique: two concurrent requests can both pass it. The unique index is
        // what actually enforces uniqueness, and CatalogExceptionHandler translates
        // the resulting 23505 into the same 409 the caller would otherwise have got.
        var slugTaken = await dbContext.Categories
            .AnyAsync(category => category.Slug == slug, cancellationToken);

        if (slugTaken)
        {
            throw CatalogErrors.CategorySlugTaken(slug);
        }

        var category = Category.Create(request.Name, slug);

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CategoryResponse.From(category);
    }
}
