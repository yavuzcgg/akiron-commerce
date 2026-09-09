using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.UpdatePriceGroup;

public sealed class UpdatePriceGroupHandler(ICatalogDbContext dbContext)
{
    public async Task<PriceGroupResponse> HandleAsync(
        string? rawCode,
        UpdatePriceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(rawCode);

        var priceGroup = await dbContext.PriceGroups
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        var discount = request.DiscountPercentage is { } percentage
            ? DiscountPercentage.Create(percentage)
            : DiscountPercentage.None;

        priceGroup.Update(request.Name, discount);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PriceGroupResponse.From(priceGroup);
    }
}
