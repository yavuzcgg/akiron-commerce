using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Pricing;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Pricing;

/// <summary>
/// The pricing rules, pinned down. These are the assertions that would catch a change in
/// what a dealer is charged, which is the kind of bug nobody notices until an invoice is
/// disputed.
/// </summary>
public sealed class PriceResolverTests
{
    private static Money Lira(decimal amount) => Money.Create(amount, Currency.TRY);

    private static readonly decimal[] ThreeTenPercentLevels = [10m, 10m, 10m];
    private static readonly decimal[] FiveSevenAndAHalfPercentLevels = [7.5m, 7.5m, 7.5m, 7.5m, 7.5m];

    private static MarkupChain Chain(params decimal[] markups) =>
        MarkupChain.Create(markups.Select(Markup.Create));

    /// <summary>Applies the levels one at a time, rounding after each — the wrong way, kept here to compare against.</summary>
    private static decimal RoundAtEveryStep(decimal start, decimal[] markups) =>
        markups.Aggregate(start, (running, markup) =>
            decimal.Round(running * (1m + (markup / 100m)), 2, MidpointRounding.AwayFromZero));

    [Fact]
    public void WithNoDiscountAndNoMarkups_ThePriceIsTheListPrice()
    {
        var quote = PriceResolver.Resolve(Lira(100m), agreedPrice: null, DiscountPercentage.None, MarkupChain.Empty);

        quote.FinalPrice.Should().Be(Lira(100m));
        quote.Basis.Should().Be(PriceBasis.ListPrice);
    }

    [Fact]
    public void AGroupDiscountComesOffTheListPrice()
    {
        var quote = PriceResolver.Resolve(
            Lira(100m), agreedPrice: null, DiscountPercentage.Create(20m), MarkupChain.Empty);

        quote.BasePrice.Should().Be(Lira(80m));
        quote.FinalPrice.Should().Be(Lira(80m));
        quote.Basis.Should().Be(PriceBasis.GroupDiscount);
    }

    [Fact]
    public void AnAgreedPriceBeatsTheGroupDiscount()
    {
        // The whole point of a price list entry: a deal was struck for this product, and
        // it overrides whatever the tier would otherwise have given.
        var quote = PriceResolver.Resolve(
            Lira(100m), agreedPrice: Lira(65m), DiscountPercentage.Create(20m), MarkupChain.Empty);

        quote.BasePrice.Should().Be(Lira(65m));
        quote.Basis.Should().Be(PriceBasis.AgreedPrice);
    }

    [Fact]
    public void ASingleMarkupGoesOnTop()
    {
        var quote = PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(10m));

        quote.FinalPrice.Should().Be(Lira(110m));
    }

    [Fact]
    public void TwoMarkupsCompoundRatherThanAddingUp()
    {
        // 121, not 120. The sub-dealer marks up the price they pay, which already carries
        // the first dealer's margin. Adding the percentages instead would understate every
        // price down a chain of more than one level.
        var quote = PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(10m, 10m));

        quote.FinalPrice.Should().Be(Lira(121m));
    }

    [Fact]
    public void RoundingHappensOnceAtTheEndRatherThanAtEveryLevel()
    {
        // Rounded per level this gives 133.11: 100 -> 110.00 -> 121.00 -> 133.10 -> ...
        // Multiplied first and rounded once it is 133.10. The gap is small here and grows
        // with the chain and the price, which is exactly why it must not be left to drift.
        var quote = PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(10m, 10m, 10m));

        var roundedAtEveryStep = RoundAtEveryStep(100m, ThreeTenPercentLevels);

        quote.FinalPrice.Amount.Should().Be(133.10m);
        quote.FinalPrice.Amount.Should().Be(roundedAtEveryStep,
            "these agree at three levels; the divergence appears with fractions, which the next test covers");
    }

    [Fact]
    public void IntermediateRoundingWouldDriftAwayFromTheSingleRounding()
    {
        // A real case, found by searching the price range this catalogue actually sells in:
        // 1000.02 through five 7.5% levels comes to 1435.66 rounded once, and 1435.65
        // rounded at every level. A kuruş per line, on every order, in whichever direction
        // the fractions happen to fall — which is what makes it a discrepancy rather than
        // an error anyone can predict.
        var quote = PriceResolver.Resolve(
            Lira(1000.02m), null, DiscountPercentage.None, Chain(7.5m, 7.5m, 7.5m, 7.5m, 7.5m));

        var roundedAtEveryStep = RoundAtEveryStep(1000.02m, FiveSevenAndAHalfPercentLevels);

        quote.FinalPrice.Amount.Should().Be(1435.66m);
        roundedAtEveryStep.Should().Be(1435.65m);
        quote.FinalPrice.Amount.Should().NotBe(roundedAtEveryStep,
            "this is the drift the single rounding exists to avoid");
    }

    [Fact]
    public void HalfKurusRoundsAwayFromZero() =>
        // 100.005 -> 100.01, not the banker's 100.00, which is what Turkish retail expects.
        PriceResolver.Resolve(Lira(100.01m), null, DiscountPercentage.None, Chain(0m))
            .FinalPrice.Amount.Should().Be(100.01m);

    [Fact]
    public void AZeroMarkupChangesNothing() =>
        PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(0m))
            .FinalPrice.Should().Be(Lira(100m));

    [Fact]
    public void AHundredPercentMarkupDoublesThePrice() =>
        PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(100m))
            .FinalPrice.Should().Be(Lira(200m));

    [Fact]
    public void TheCurrencySurvivesTheWholeCalculation() =>
        PriceResolver.Resolve(
            Money.Create(100m, Currency.EUR), null, DiscountPercentage.Create(10m), Chain(5m))
            .FinalPrice.Currency.Should().Be(Currency.EUR);

    [Fact]
    public void MoreMarkupsNeverLowerThePrice()
    {
        var one = PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(5m)).FinalPrice.Amount;
        var two = PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.None, Chain(5m, 5m)).FinalPrice.Amount;

        two.Should().BeGreaterThan(one);
    }

    [Fact]
    public void TheBreakdownReportsEveryMarkupItApplied()
    {
        var quote = PriceResolver.Resolve(Lira(100m), null, DiscountPercentage.Create(10m), Chain(5m, 7m));

        quote.ListPrice.Should().Be(Lira(100m));
        quote.BasePrice.Should().Be(Lira(90m));
        quote.AppliedMarkups.Select(markup => markup.Value).Should().ContainInOrder([5m, 7m]);
    }
}

public sealed class MarkupChainTests
{
    [Fact]
    public void AChainDeeperThanTheLimit_IsRejected() =>
        FluentActions.Invoking(() => MarkupChain.Create(
                Enumerable.Repeat(Markup.Create(1m), MarkupChain.MaxDepth + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(CatalogErrorCodes.MarkupChainTooDeep);

    [Fact]
    public void AChainAtExactlyTheLimit_IsAccepted() =>
        MarkupChain.Create(Enumerable.Repeat(Markup.Create(1m), MarkupChain.MaxDepth))
            .Depth.Should().Be(MarkupChain.MaxDepth);

    [Fact]
    public void AnEmptyChainMultipliesByOne() =>
        MarkupChain.Empty.Multiplier.Should().Be(1m);

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void ANegativeOrOversizedMarkup_IsRejected(decimal value) =>
        // Negative is not a discount by another name: it would let a dealer sell below the
        // price they were given without it showing anywhere.
        FluentActions.Invoking(() => Markup.Create(value))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(CatalogErrorCodes.MarkupOutOfRange);

    [Fact]
    public void AMarkupWithMoreThanTwoDecimals_IsRejected() =>
        FluentActions.Invoking(() => Markup.Create(7.555m))
            .Should().Throw<DomainValidationException>();
}
