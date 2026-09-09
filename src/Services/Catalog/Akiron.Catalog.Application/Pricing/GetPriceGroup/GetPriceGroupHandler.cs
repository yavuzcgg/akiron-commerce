using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.GetPriceGroup;

public sealed class GetPriceGroupHandler(ICatalogDbContext dbContext)
{
    public async Task<PriceGroupResponse> HandleAsync(string? rawCode, CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(rawCode);

        var priceGroup = await dbContext.PriceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        return PriceGroupResponse.From(priceGroup);
    }
}
