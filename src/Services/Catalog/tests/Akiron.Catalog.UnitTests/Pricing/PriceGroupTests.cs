using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Pricing;

public sealed class PriceGroupCodeTests
{
    [Theory]
    [InlineData("BAYI-A")]
    [InlineData("TOPTAN")]
    [InlineData("E-TICARET-2026")]
    public void Create_WithWellFormedValue_KeepsIt(string value) =>
        PriceGroupCode.Create(value).Value.Should().Be(value);

    [Fact]
    public void Create_UpperCasesTheValue() =>
        PriceGroupCode.Create("bayi-a").Value.Should().Be("BAYI-A");

    [Fact]
    public void Create_TreatsDifferentCasingAsTheSameCode() =>
        PriceGroupCode.Create("bayi-a").Should().Be(PriceGroupCode.Create("BAYI-A"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]            // shorter than the minimum
    [InlineData("BAYI A")]       // space
    [InlineData("-BAYI")]        // leading hyphen
    [InlineData("BAYI-")]        // trailing hyphen
    [InlineData("BAYI--A")]      // doubled hyphen
    [InlineData("BAYİ")]         // non-ascii
    public void Create_WithMalformedValue_Throws(string? value) =>
        FluentActions.Invoking(() => PriceGroupCode.Create(value))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(CatalogErrorCodes.PriceGroupCodeInvalidFormat);
}

public sealed class DiscountPercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(17.5)]
    [InlineData(100)]
    public void Create_WithinRange_KeepsTheValue(decimal value) =>
        DiscountPercentage.Create(value).Value.Should().Be(value);

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Create_OutsideRange_Throws(decimal value) =>
        FluentActions.Invoking(() => DiscountPercentage.Create(value))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(CatalogErrorCodes.DiscountOutOfRange);

    [Fact]
    public void Create_WithMoreThanTwoDecimals_ThrowsRatherThanRounding() =>
        // Same rule as Money: a discount quietly changed by a hundredth of a percent is a
        // discrepancy nobody can trace back afterwards.
        FluentActions.Invoking(() => DiscountPercentage.Create(12.345m))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void None_IsZeroAndKnowsIt()
    {
        DiscountPercentage.None.Value.Should().Be(0m);
        DiscountPercentage.None.IsNone.Should().BeTrue();
    }
}

public sealed class PriceGroupTests
{
    private static readonly PriceGroupCode AnyCode = PriceGroupCode.Create("BAYI-A");

    private static PriceGroup CreateGroup(Currency currency = Currency.TRY) =>
        PriceGroup.Create(AnyCode, "Bayi A", currency, DiscountPercentage.Create(10m));

    [Fact]
    public void Create_StoresWhatItWasGiven()
    {
        var group = CreateGroup();

        group.Code.Should().Be(AnyCode);
        group.Currency.Should().Be(Currency.TRY);
        group.Discount.Value.Should().Be(10m);
        group.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithAnUndefinedCurrency_Throws() =>
        FluentActions.Invoking(() => PriceGroup.Create(AnyCode, "Bayi A", (Currency)0, DiscountPercentage.None))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(CatalogErrorCodes.MoneyUnsupportedCurrency);

    [Fact]
    public void Update_ChangesTheNameAndDiscountOnly()
    {
        var group = CreateGroup();

        group.Update("Bayi A - guncel", DiscountPercentage.Create(20m));

        group.Name.Should().Be("Bayi A - guncel");
        group.Discount.Value.Should().Be(20m);
        group.Currency.Should().Be(Currency.TRY, "the currency is fixed once prices exist in it");
        group.Code.Should().Be(AnyCode);
        group.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void PriceListEntry_MustBeInTheCurrencyOfItsGroup()
    {
        // The API never lets this happen — the amount is put into the group's currency by
        // the handler — so this guard catches a programming mistake, not bad input.
        var group = CreateGroup(Currency.TRY);
        var dollars = Money.Create(100m, Currency.USD);

        FluentActions.Invoking(() => PriceListEntry.Create(group, ProductId.New(), dollars))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be(CatalogErrorCodes.PriceCurrencyMismatch);
    }

    [Fact]
    public void PriceListEntry_ChangePrice_StampsTheUpdateTime()
    {
        var group = CreateGroup();
        var entry = PriceListEntry.Create(group, ProductId.New(), Money.Create(3800m, Currency.TRY));

        entry.ChangePrice(group, Money.Create(3700m, Currency.TRY));

        entry.Price.Amount.Should().Be(3700m);
        entry.UpdatedAt.Should().NotBeNull();
    }
}
