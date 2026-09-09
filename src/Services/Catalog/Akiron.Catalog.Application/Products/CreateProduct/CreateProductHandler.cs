using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Products.CreateProduct;

public sealed class CreateProductHandler(ICatalogDbContext dbContext)
{
    public async Task<ProductResponse> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var sku = Sku.Create(request.Sku);
        var basePrice = Money.Create(request.BasePrice, Enum.Parse<Currency>(request.Currency!, ignoreCase: true));
        var categoryId = new CategoryId(request.CategoryId);

        // A product without a category would be unreachable in the storefront, so an
        // unknown category is a 404 rather than a silently orphaned row. The foreign key
        // still backs this up if the category disappears between here and SaveChanges.
        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException($"Category '{categoryId}' was not found.");
        }

        // Courtesy check only. Under concurrency the unique index is what actually keeps
        // SKUs unique, and CatalogExceptionHandler turns its violation into the same 409.
        var skuTaken = await dbContext.Products
            .AnyAsync(product => product.Sku == sku, cancellationToken);

        if (skuTaken)
        {
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        }

        var product = Product.Create(sku, request.Name, request.Description, categoryId, basePrice);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ProductResponse.From(product);
    }
}
