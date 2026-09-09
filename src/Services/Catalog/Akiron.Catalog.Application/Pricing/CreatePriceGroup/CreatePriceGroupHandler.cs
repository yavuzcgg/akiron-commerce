using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.CreatePriceGroup;

public sealed class CreatePriceGroupHandler(ICatalogDbContext dbContext)
{
    public async Task<PriceGroupResponse> HandleAsync(
        CreatePriceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(request.Code);
        var currency = Enum.Parse<Currency>(request.Currency!, ignoreCase: true);
        var discount = request.DiscountPercentage is { } percentage
            ? DiscountPercentage.Create(percentage)
            : DiscountPercentage.None;

        // Courtesy check; the unique index is what holds under concurrency.
        var codeTaken = await dbContext.PriceGroups
            .AnyAsync(priceGroup => priceGroup.Code == code, cancellationToken);

        if (codeTaken)
        {
            throw CatalogErrors.PriceGroupCodeTaken(code);
        }

        var priceGroup = PriceGroup.Create(code, request.Name, currency, discount);

        dbContext.PriceGroups.Add(priceGroup);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PriceGroupResponse.From(priceGroup);
    }
}
