using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.DeletePriceGroup;

public sealed class DeletePriceGroupHandler(ICatalogDbContext dbContext)
{
    public async Task HandleAsync(string? rawCode, CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(rawCode);

        var priceGroup = await dbContext.PriceGroups
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        // No pre-check for prices: the foreign key restricts the delete, so the database
        // refuses it and the exception handler turns that into a 409. Checking here would
        // only add a race the constraint already wins.
        dbContext.PriceGroups.Remove(priceGroup);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
