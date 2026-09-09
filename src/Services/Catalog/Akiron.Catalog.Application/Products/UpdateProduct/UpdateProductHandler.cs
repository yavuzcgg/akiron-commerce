using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Products.UpdateProduct;

public sealed class UpdateProductHandler(ICatalogDbContext dbContext)
{
    public async Task<ProductResponse> HandleAsync(
        ProductId productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        // Tracked on purpose: the entity's own Update method applies the change, so EF
        // works out the SQL from what actually differs.
        var product = await dbContext.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw CatalogErrors.ProductNotFound(productId);

        var categoryId = new CategoryId(request.CategoryId);

        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            throw CatalogErrors.CategoryNotFound(categoryId);
        }

        var basePrice = Money.Create(request.BasePrice, Enum.Parse<Currency>(request.Currency!, ignoreCase: true));

        product.Update(request.Name, request.Description, categoryId, basePrice, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ProductResponse.From(product);
    }
}
