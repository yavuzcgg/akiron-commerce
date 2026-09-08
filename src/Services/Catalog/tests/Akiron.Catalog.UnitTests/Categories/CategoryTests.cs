using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Categories;

public sealed class CategoryTests
{
    private static readonly Slug AnySlug = Slug.Create("kis-lastikleri");

    [Fact]
    public void Create_StampsAnIdAndCreationTime()
    {
        var before = DateTimeOffset.UtcNow;

        var category = Category.Create("Kış Lastikleri", AnySlug);

        category.Id.Value.Should().NotBe(Guid.Empty);
        category.CreatedAt.Should().BeOnOrAfter(before);
        category.UpdatedAt.Should().BeNull("a category that was never edited has no update time");
    }

    [Fact]
    public void Create_TrimsTheName() =>
        Category.Create("  Kış Lastikleri  ", AnySlug).Name.Should().Be("Kış Lastikleri");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name) =>
        FluentActions.Invoking(() => Category.Create(name, AnySlug))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Create_WithAnOverlongName_Throws() =>
        FluentActions.Invoking(() => Category.Create(new string('a', Category.MaxNameLength + 1), AnySlug))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Rename_ChangesTheNameAndRecordsWhen()
    {
        var category = Category.Create("Kış Lastikleri", AnySlug);

        category.Rename("Kışlık Lastikler");

        category.Name.Should().Be("Kışlık Lastikler");
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void NewIds_AreVersion7SoTheIndexAppendsInsteadOfSplitting()
    {
        // Version 7 embeds a timestamp, which is what keeps the primary key index
        // appending rather than inserting at random positions. Asserting the version is
        // deterministic; asserting that two ids compare in order is not, because ids
        // minted inside the same millisecond differ only in random bits.
        CategoryId.New().Value.Version.Should().Be(7);
    }

    [Fact]
    public void NewIds_MintedInDifferentMilliseconds_SortInCreationOrder()
    {
        var first = CategoryId.New();
        Thread.Sleep(2);
        var second = CategoryId.New();

        first.Value.CompareTo(second.Value).Should().BeLessThan(0);
    }

    [Fact]
    public void Create_StampsATimePostgresCanStoreExactly()
    {
        // timestamptz keeps microseconds. If the entity kept .NET's finer ticks, the
        // object returned by a create call would differ from the one read back.
        var category = Category.Create("Kış Lastikleri", AnySlug);

        (category.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond).Should().Be(0);
    }
}
