using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Products.GetProduct;

public sealed class GetProductHandler(ICatalogDbContext dbContext)
{
    public async Task<ProductResponse> HandleAsync(ProductId productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken)
            ?? throw new NotFoundException($"Product '{productId}' was not found.");

        return ProductResponse.From(product);
    }
}
