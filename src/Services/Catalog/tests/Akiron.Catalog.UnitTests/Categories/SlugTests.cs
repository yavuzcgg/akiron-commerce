using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Categories;

/// <summary>
/// Slug is the first value object in the codebase, so these tests also pin down the
/// house rule it demonstrates: a value object either holds a valid value or does not
/// exist. Nothing downstream is allowed to re-check the format.
/// </summary>
public sealed class SlugTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("kis-lastikleri")]
    [InlineData("yaz-lastikleri-2026")]
    [InlineData("205-55-r16")]
    public void Create_WithWellFormedValue_KeepsIt(string value) =>
        Slug.Create(value).Value.Should().Be(value);

    [Fact]
    public void Create_TrimsSurroundingWhitespace() =>
        Slug.Create("  kis-lastikleri  ").Value.Should().Be("kis-lastikleri");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Kis-Lastikleri")]   // uppercase
    [InlineData("kis lastikleri")]   // space
    [InlineData("-kis")]             // leading hyphen
    [InlineData("kis-")]             // trailing hyphen
    [InlineData("kis--lastikleri")]  // doubled hyphen
    [InlineData("kış-lastikleri")]   // non-ascii
    public void Create_WithMalformedValue_Throws(string? value) =>
        FluentActions.Invoking(() => Slug.Create(value))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Create_WithValueOverTheLengthLimit_Throws() =>
        FluentActions.Invoking(() => Slug.Create(new string('a', Slug.MaxLength + 1)))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Create_AtExactlyTheLengthLimit_Succeeds() =>
        Slug.Create(new string('a', Slug.MaxLength)).Value.Should().HaveLength(Slug.MaxLength);

    [Fact]
    public void Slugs_WithTheSameText_AreEqual() =>
        Slug.Create("kis-lastikleri").Should().Be(Slug.Create("kis-lastikleri"));
}
