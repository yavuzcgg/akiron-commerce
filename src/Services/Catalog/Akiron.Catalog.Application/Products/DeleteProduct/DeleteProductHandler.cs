using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Products.DeleteProduct;

public sealed class DeleteProductHandler(ICatalogDbContext dbContext)
{
    public async Task HandleAsync(ProductId productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw CatalogErrors.ProductNotFound(productId);

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
