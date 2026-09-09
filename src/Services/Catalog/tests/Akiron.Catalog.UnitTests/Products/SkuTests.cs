using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Products;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Products;

/// <summary>
/// The normalisation rules here are what keep the unique index honest: if two spellings
/// of one SKU could both be stored, the catalogue would carry duplicate products.
/// </summary>
public sealed class SkuTests
{
    [Theory]
    [InlineData("ABC")]
    [InlineData("MICH-205-55-R16")]
    [InlineData("PIR-2026")]
    public void Create_WithWellFormedValue_KeepsIt(string value) =>
        Sku.Create(value).Value.Should().Be(value);

    [Fact]
    public void Create_UpperCasesTheValue() =>
        Sku.Create("mich-205-55-r16").Value.Should().Be("MICH-205-55-R16");

    [Fact]
    public void Create_TrimsSurroundingWhitespace() =>
        Sku.Create("  ABC-1  ").Value.Should().Be("ABC-1");

    [Fact]
    public void Create_TreatsDifferentCasingAsTheSameSku() =>
        Sku.Create("abc-1").Should().Be(Sku.Create("ABC-1"));

    [Fact]
    public void Create_UsesInvariantCasing()
    {
        // Under tr-TR, culture-aware upper-casing turns 'i' into a dotted capital, which
        // would make the same SKU normalise differently on a Turkish machine than on the
        // server. Pinning the current culture proves the invariant path is taken.
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");

        try
        {
            Sku.Create("michelin").Value.Should().Be("MICHELIN");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AB")]              // shorter than the minimum
    [InlineData("ABC 123")]         // space
    [InlineData("-ABC")]            // leading hyphen
    [InlineData("ABC-")]            // trailing hyphen
    [InlineData("ABC--1")]          // doubled hyphen
    [InlineData("ABÇ")]             // non-ascii
    public void Create_WithMalformedValue_Throws(string? value) =>
        FluentActions.Invoking(() => Sku.Create(value))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Create_WithValueOverTheLengthLimit_Throws() =>
        FluentActions.Invoking(() => Sku.Create(new string('A', Sku.MaxLength + 1)))
            .Should().Throw<DomainValidationException>();
}
