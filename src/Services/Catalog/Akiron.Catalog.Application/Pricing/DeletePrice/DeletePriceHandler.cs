using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.DeletePrice;

public sealed class DeletePriceHandler(ICatalogDbContext dbContext)
{
    public async Task HandleAsync(string? rawCode, ProductId productId, CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(rawCode);

        var priceGroup = await dbContext.PriceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        var entry = await dbContext.PriceListEntries
            .SingleOrDefaultAsync(
                candidate => candidate.PriceGroupId == priceGroup.Id && candidate.ProductId == productId,
                cancellationToken)
            ?? throw CatalogErrors.PriceListEntryNotFound(code, productId);

        dbContext.PriceListEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
