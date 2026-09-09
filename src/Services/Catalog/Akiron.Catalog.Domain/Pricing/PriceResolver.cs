namespace Akiron.Catalog.Domain.Pricing;

/// <summary>Where the price before markups came from.</summary>
public enum PriceBasis
{
    /// <summary>Nothing group-specific applied; this is the list price.</summary>
    ListPrice = 1,

    /// <summary>The group discount was taken off the list price.</summary>
    GroupDiscount = 2,

    /// <summary>An agreed price was stored for this product in this group.</summary>
    AgreedPrice = 3,
}

/// <summary>
/// A resolved price and how it was arrived at.
/// </summary>
/// <remarks>
/// The breakdown is part of the answer, not decoration. A dealer asking why they are
/// charged what they are charged should get a reply, and the same fields become the
/// contract the Order service locks into an order in Faz 5.
/// </remarks>
public sealed record PriceQuote(
    Money ListPrice,
    Money BasePrice,
    PriceBasis Basis,
    IReadOnlyList<Markup> AppliedMarkups,
    Money FinalPrice);

/// <summary>
/// Turns a list price into what a particular buyer pays.
/// </summary>
/// <remarks>
/// Pure: it takes everything it needs as arguments and touches no database, which is
/// what lets the rules below be pinned down by fast unit tests rather than inferred from
/// an endpoint.
/// </remarks>
public static class PriceResolver
{
    /// <summary>
    /// Resolution order: an agreed price for this product wins; failing that the group
    /// discount comes off the list price; failing that the list price stands. The markup
    /// chain then compounds on top of whichever it was.
    /// </summary>
    public static PriceQuote Resolve(
        Money listPrice,
        Money? agreedPrice,
        DiscountPercentage groupDiscount,
        MarkupChain markupChain)
    {
        var (basePrice, basis) = ResolveBasis(listPrice, agreedPrice, groupDiscount);

        // Multiplied unrounded, then rounded once. Rounding after every level of the
        // chain would drift, and the drift compounds along with the price.
        var finalAmount = Round(basePrice.Amount * markupChain.Multiplier);

        return new PriceQuote(
            listPrice,
            basePrice,
            basis,
            markupChain.Markups,
            Money.Create(finalAmount, basePrice.Currency));
    }

    private static (Money BasePrice, PriceBasis Basis) ResolveBasis(
        Money listPrice,
        Money? agreedPrice,
        DiscountPercentage groupDiscount)
    {
        if (agreedPrice is not null)
        {
            return (agreedPrice, PriceBasis.AgreedPrice);
        }

        if (groupDiscount.IsNone)
        {
            return (listPrice, PriceBasis.ListPrice);
        }

        var discounted = Round(listPrice.Amount * (1m - (groupDiscount.Value / 100m)));

        return (Money.Create(discounted, listPrice.Currency), PriceBasis.GroupDiscount);
    }

    /// <summary>
    /// Half away from zero, which is what Turkish retail expects: 0.005 becomes 0.01, not
    /// the banker's 0.00.
    /// </summary>
    private static decimal Round(decimal amount) =>
        decimal.Round(amount, Money.DecimalPlaces, MidpointRounding.AwayFromZero);
}
