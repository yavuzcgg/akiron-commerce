using System.Diagnostics;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Application.Pricing.QuotePrices;

/// <summary>
/// Asks what a particular buyer pays for a set of products.
/// </summary>
/// <remarks>
/// The markup chain arrives in the request because the dealer hierarchy that produces it
/// lives in Identity, which does not exist until Faz 2. When it does, the same
/// calculation runs with the chain read from the dealer rather than sent by the caller —
/// which is also why prices are never trusted from a request (ADR-0012).
/// </remarks>
public sealed record QuotePricesRequest(
    string? PriceGroupCode,
    IReadOnlyList<decimal>? Markups,
    IReadOnlyList<Guid>? ProductIds);

public sealed class QuotePricesRequestValidator : AbstractValidator<QuotePricesRequest>
{
    public QuotePricesRequestValidator()
    {
        RuleFor(request => request.ProductIds)
            .NotEmpty()
            .WithMessage("'{PropertyName}' must name at least one product.");

        RuleFor(request => request.ProductIds!.Count)
            .LessThanOrEqualTo(MaxProducts)
            .When(request => request.ProductIds is not null)
            .OverridePropertyName(nameof(QuotePricesRequest.ProductIds))
            .WithMessage($"'{{PropertyName}}' must name at most {MaxProducts} products in one call.");

        RuleFor(request => request.Markups!.Count)
            .LessThanOrEqualTo(MarkupChain.MaxDepth)
            .When(request => request.Markups is not null)
            .OverridePropertyName(nameof(QuotePricesRequest.Markups))
            .WithMessage($"'{{PropertyName}}' may hold at most {MarkupChain.MaxDepth} levels.");
    }

    /// <summary>A checkout basket, not a catalogue export; the list endpoint is for browsing.</summary>
    public const int MaxProducts = 100;
}

public sealed record QuotedPriceResponse(
    ProductId ProductId,
    string Sku,
    decimal ListPrice,
    decimal BasePrice,
    string Basis,
    IReadOnlyList<decimal> AppliedMarkups,
    decimal FinalPrice,
    string Currency);

public sealed class QuotePricesHandler(ICatalogDbContext dbContext)
{
    public async Task<IReadOnlyList<QuotedPriceResponse>> HandleAsync(
        QuotePricesRequest request,
        CancellationToken cancellationToken)
    {
        var markupChain = MarkupChain.Create(request.Markups?.Select(Markup.Create));
        var productIds = request.ProductIds!.Select(id => new ProductId(id)).ToArray();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);

        var missing = productIds.Except(products.Select(product => product.Id)).ToArray();

        if (missing.Length > 0)
        {
            // Answering for the products that do exist and quietly dropping the rest would
            // let a basket be priced without its most expensive line.
            throw CatalogErrors.ProductNotFound(missing[0]);
        }

        var priceGroup = await ResolvePriceGroupAsync(request.PriceGroupCode, cancellationToken);
        var agreedPrices = await LoadAgreedPricesAsync(priceGroup, productIds, cancellationToken);

        using var activity = CatalogActivitySource.Instance.StartActivity(CatalogActivitySource.ResolvePrices);
        activity?.SetTag("catalog.price_group", priceGroup?.Code.Value ?? "none");
        activity?.SetTag("catalog.markup_depth", markupChain.Depth);
        activity?.SetTag("catalog.product_count", products.Count);

        return
        [
            .. products.Select(product =>
            {
                agreedPrices.TryGetValue(product.Id, out var agreedPrice);

                var quote = PriceResolver.Resolve(
                    product.BasePrice,
                    agreedPrice,
                    priceGroup?.Discount ?? DiscountPercentage.None,
                    markupChain);

                return new QuotedPriceResponse(
                    product.Id,
                    product.Sku.Value,
                    quote.ListPrice.Amount,
                    quote.BasePrice.Amount,
                    quote.Basis.ToString(),
                    [.. quote.AppliedMarkups.Select(markup => markup.Value)],
                    quote.FinalPrice.Amount,
                    quote.FinalPrice.Currency.ToString());
            }),
        ];
    }

    private async Task<PriceGroup?> ResolvePriceGroupAsync(string? rawCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return null;
        }

        var code = PriceGroupCode.Create(rawCode);

        return await dbContext.PriceGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Code == code, cancellationToken)
            ?? throw CatalogErrors.PriceGroupNotFound(code);
    }

    private async Task<Dictionary<ProductId, Money>> LoadAgreedPricesAsync(
        PriceGroup? priceGroup,
        ProductId[] productIds,
        CancellationToken cancellationToken)
    {
        if (priceGroup is null)
        {
            return [];
        }

        // One query for the whole basket rather than one per line: the same lookup runs on
        // every storefront page that shows prices.
        var entries = await dbContext.PriceListEntries
            .AsNoTracking()
            .Where(entry => entry.PriceGroupId == priceGroup.Id && productIds.Contains(entry.ProductId))
            .ToListAsync(cancellationToken);

        return entries.ToDictionary(entry => entry.ProductId, entry => entry.Price);
    }
}
