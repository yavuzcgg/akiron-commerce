using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.UpsertPrice;

/// <summary>The currency is absent on purpose: it belongs to the price group, so it cannot disagree.</summary>
public sealed record UpsertPriceRequest(decimal Amount);

public sealed class UpsertPriceRequestValidator : AbstractValidator<UpsertPriceRequest>
{
    public UpsertPriceRequestValidator() =>
        RuleFor(request => request.Amount)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, Money.DecimalPlaces, ignoreTrailingZeros: true)
            .WithMessage($"'{{PropertyName}}' must have at most {Money.DecimalPlaces} decimal places.");
}

/// <summary>Whether the price was created or replaced, so the endpoint can answer 201 or 200.</summary>
public sealed record UpsertPriceResult(PriceListEntryResponse Price, bool Created);

public sealed class UpsertPriceHandler(ICatalogDbContext dbContext, IPriceListWriter priceListWriter)
{
    public async Task<UpsertPriceResult> HandleAsync(
        string? rawCode,
        ProductId productId,
        UpsertPriceRequest request,
        CancellationToken cancellationToken)
    {
        var code = PriceGroupCode.Create(rawCode);

        var priceGroup = await dbContext.PriceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);

        var productExists = await dbContext.Products
            .AnyAsync(product => product.Id == productId, cancellationToken);

        if (!productExists)
        {
            throw CatalogErrors.ProductNotFound(productId);
        }

        // Built in the group's currency rather than one the caller chose, which is what
        // makes a mixed-currency price list impossible. Because the currency can only
        // come from here, the invariant PriceListEntry guards is satisfied by
        // construction — which is what makes it safe for the writer below to bypass the
        // entity and go straight to a single statement.
        var price = Money.Create(request.Amount, priceGroup.Currency);

        // Read-then-write was the obvious way to write this, and it is wrong: two callers
        // can both find nothing, both try to insert, and the composite key rejects the
        // loser with a 409 — breaking the promise PUT makes about being idempotent. The
        // writer settles it in one statement, leaving no window to race through.
        var outcome = await priceListWriter.UpsertAsync(
            priceGroup.Id, productId, price, Timestamp.UtcNow(), cancellationToken);

        var response = new PriceListEntryResponse(
            productId,
            price.Amount,
            price.Currency.ToString(),
            outcome.CreatedAt,
            outcome.UpdatedAt);

        return new UpsertPriceResult(response, outcome.Inserted);
    }
}
