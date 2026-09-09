using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Common;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using AwesomeAssertions;

namespace Akiron.Catalog.UnitTests.Products;

public sealed class ProductTests
{
    private static readonly Sku AnySku = Sku.Create("MICH-205-55-R16");
    private static readonly Money AnyPrice = Money.Create(4250.00m, Currency.TRY);
    private static readonly CategoryId AnyCategory = CategoryId.New();

    private static Product CreateProduct() =>
        Product.Create(AnySku, "Michelin Primacy 4", "Yaz lastiği", AnyCategory, AnyPrice);

    [Fact]
    public void Create_StartsActiveAndUnedited()
    {
        var product = CreateProduct();

        product.IsActive.Should().BeTrue("a new product is sellable unless someone says otherwise");
        product.UpdatedAt.Should().BeNull();
        product.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TrimsTextAndKeepsTheRest()
    {
        var product = Product.Create(AnySku, "  Michelin  ", "  Yaz  ", AnyCategory, AnyPrice);

        product.Name.Should().Be("Michelin");
        product.Description.Should().Be("Yaz");
        product.BasePrice.Should().Be(AnyPrice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name) =>
        FluentActions.Invoking(() => Product.Create(AnySku, name, null, AnyCategory, AnyPrice))
            .Should().Throw<DomainValidationException>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankDescription_StoresNull(string? description) =>
        Product.Create(AnySku, "Michelin", description, AnyCategory, AnyPrice)
            .Description.Should().BeNull("a blank description and no description are the same thing");

    [Fact]
    public void Create_WithAnOverlongName_Throws() =>
        FluentActions.Invoking(() =>
                Product.Create(AnySku, new string('a', Product.MaxNameLength + 1), null, AnyCategory, AnyPrice))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Update_ChangesTheEditableFieldsAndStampsTheTime()
    {
        var product = CreateProduct();
        var newCategory = CategoryId.New();
        var newPrice = Money.Create(4990.50m, Currency.TRY);

        product.Update("Michelin Primacy 5", null, newCategory, newPrice, isActive: false);

        product.Name.Should().Be("Michelin Primacy 5");
        product.Description.Should().BeNull();
        product.CategoryId.Should().Be(newCategory);
        product.BasePrice.Should().Be(newPrice);
        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_LeavesTheSkuAlone()
    {
        // The SKU is the reference other systems store, so nothing on the entity offers
        // a way to change it. This test fails the day someone adds one.
        var product = CreateProduct();

        product.Update("Yeni ad", null, AnyCategory, AnyPrice, isActive: true);

        product.Sku.Should().Be(AnySku);
        typeof(Product).GetProperty(nameof(Product.Sku))!.SetMethod!.IsPublic
            .Should().BeFalse("the SKU must not be settable from outside the entity");
    }

    [Fact]
    public void CreatedAt_IsStoredAtAPrecisionPostgresKeeps() =>
        (CreateProduct().CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond).Should().Be(0);
}
