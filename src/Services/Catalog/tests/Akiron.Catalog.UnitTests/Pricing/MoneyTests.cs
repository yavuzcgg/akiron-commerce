using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Pricing;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Pricing;

public sealed class MoneyTests
{
    [Fact]
    public void Create_KeepsTheAmountAndCurrency()
    {
        var money = Money.Create(1299.90m, Currency.TRY);

        money.Amount.Should().Be(1299.90m);
        money.Currency.Should().Be(Currency.TRY);
    }

    [Fact]
    public void Create_AllowsZero() =>
        Money.Create(0m, Currency.TRY).Amount.Should().Be(0m);

    [Fact]
    public void Create_WithNegativeAmount_Throws() =>
        FluentActions.Invoking(() => Money.Create(-0.01m, Currency.TRY))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Create_WithMoreThanTwoDecimals_ThrowsRatherThanRounding()
    {
        // Rounding here would be silent: the caller would be charged a price they never
        // sent. Refusing forces the decision back to whoever knows the real price.
        FluentActions.Invoking(() => Money.Create(10.005m, Currency.TRY))
            .Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithTrailingZeros_IsAccepted() =>
        Money.Create(10.100m, Currency.TRY).Amount.Should().Be(10.10m);

    [Fact]
    public void Create_WithUndefinedCurrency_Throws() =>
        FluentActions.Invoking(() => Money.Create(1m, (Currency)0))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void DefaultCurrency_IsNotAValidMember() =>
        // Zero is deliberately unused so a Money that skipped the factory cannot pass for lira.
        Enum.IsDefined((Currency)0).Should().BeFalse();

    [Fact]
    public void AmountsThatDifferOnlyInTrailingZeros_AreEqual() =>
        Money.Create(10.10m, Currency.TRY).Should().Be(Money.Create(10.1m, Currency.TRY));

    [Fact]
    public void SameAmountInDifferentCurrencies_IsNotEqual() =>
        Money.Create(10m, Currency.TRY).Should().NotBe(Money.Create(10m, Currency.USD));
}
