using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Application.Pricing.QuotePrices;
using Akiron.Catalog.Application.Products;
using Akiron.Catalog.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Pricing;

/// <summary>
/// The end of the pricing story: a list price, a dealer tier and a chain of resellers
/// turning into the number a buyer is actually charged. The arithmetic itself is pinned
/// down by unit tests; these check that the right rows reach it.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class PriceQuoteTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static readonly decimal[] TwoTenPercentMarkups = [10m, 10m];
    private static readonly decimal[] SixOnePercentMarkups = [1m, 1m, 1m, 1m, 1m, 1m];

    private async Task<Guid> CreateProductAsync(HttpClient client, string sku, decimal listPrice)
    {
        var categoryResponse = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Lastikler", slug = $"lastikler-{Guid.CreateVersion7():N}" },
            TestContext.Current.CancellationToken);

        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                sku,
                name = "Michelin Primacy 4",
                description = (string?)null,
                categoryId = category!.Id.Value,
                basePrice = listPrice,
                currency = "TRY",
            },
            TestContext.Current.CancellationToken);

        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        return product!.Id.Value;
    }

    private static async Task CreatePriceGroupAsync(HttpClient client, string code, decimal discount) =>
        await client.PostAsJsonAsync(
            "/api/v1/price-groups",
            new { code, name = code, currency = "TRY", discountPercentage = discount },
            TestContext.Current.CancellationToken);

    private async Task<IReadOnlyList<QuotedPriceResponse>> QuoteAsync(
        HttpClient client, object request)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/quote", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<QuotedPriceResponse>>(
            fixture.Json, TestContext.Current.CancellationToken))!;
    }

    [Fact]
    public async Task WithNoPriceGroup_TheQuoteIsTheListPrice()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "LIST-1", 1000.00m);

        var quotes = await QuoteAsync(client, new { productIds = new[] { productId } });

        quotes.Should().ContainSingle();
        quotes[0].FinalPrice.Should().Be(1000.00m);
        quotes[0].Basis.Should().Be("ListPrice");
    }

    [Fact]
    public async Task AGroupDiscountComesOffTheListPrice()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "DISC-1", 1000.00m);
        await CreatePriceGroupAsync(client, "BAYI-A", 20m);

        var quotes = await QuoteAsync(client, new { priceGroupCode = "BAYI-A", productIds = new[] { productId } });

        quotes[0].BasePrice.Should().Be(800.00m);
        quotes[0].FinalPrice.Should().Be(800.00m);
        quotes[0].Basis.Should().Be("GroupDiscount");
    }

    [Fact]
    public async Task AnAgreedPriceOverridesTheGroupDiscount()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "AGREED-1", 1000.00m);
        await CreatePriceGroupAsync(client, "BAYI-A", 20m);

        await client.PutAsJsonAsync(
            $"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 650.00m },
            TestContext.Current.CancellationToken);

        var quotes = await QuoteAsync(client, new { priceGroupCode = "BAYI-A", productIds = new[] { productId } });

        quotes[0].BasePrice.Should().Be(650.00m, "the deal struck for this product beats the tier discount");
        quotes[0].Basis.Should().Be("AgreedPrice");
    }

    [Fact]
    public async Task TheMarkupChainCompoundsOnTopOfTheDealerPrice()
    {
        // 1000 list, 20% dealer discount, then two sub-dealers taking 10% each:
        // 800 -> 880 -> 968. Adding the markups instead would give 960.
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "CHAIN-1", 1000.00m);
        await CreatePriceGroupAsync(client, "BAYI-A", 20m);

        var quotes = await QuoteAsync(client, new
        {
            priceGroupCode = "BAYI-A",
            markups = TwoTenPercentMarkups,
            productIds = new[] { productId },
        });

        quotes[0].BasePrice.Should().Be(800.00m);
        quotes[0].FinalPrice.Should().Be(968.00m);
        quotes[0].AppliedMarkups.Should().ContainInOrder(TwoTenPercentMarkups);
    }

    [Fact]
    public async Task AChainDeeperThanTheLimit_IsRejected()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "DEEP-1", 100.00m);

        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/quote",
            new { markups = SixOnePercentMarkups, productIds = new[] { productId } },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task QuotingAProductThatDoesNotExist_IsNotFound()
    {
        // Answering only for the products that exist would let a basket be priced without
        // one of its lines, which is worse than refusing.
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "PART-1", 100.00m);

        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/quote",
            new { productIds = new[] { productId, Guid.CreateVersion7() } },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.ProductNotFound);
    }

    [Fact]
    public async Task QuotingWithAPriceGroupThatDoesNotExist_IsNotFound()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "NOGROUP-1", 100.00m);

        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/quote",
            new { priceGroupCode = "YOK-1", productIds = new[] { productId } },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TheProductListCarriesTheGroupPriceWhenAGroupIsAskedFor()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client, "LISTED-1", 1000.00m);
        await CreatePriceGroupAsync(client, "BAYI-A", 25m);

        var withoutGroup = await client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            "/api/v1/products", fixture.Json, TestContext.Current.CancellationToken);

        withoutGroup!.Items.Single(product => product.Id.Value == productId)
            .GroupPrice.Should().BeNull("no group was asked for");

        var withGroup = await client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            "/api/v1/products?priceGroup=bayi-a", fixture.Json, TestContext.Current.CancellationToken);

        var row = withGroup!.Items.Single(product => product.Id.Value == productId);
        row.BasePrice.Amount.Should().Be(1000.00m, "the list price is still reported");
        row.GroupPrice!.Amount.Should().Be(750.00m);
        row.GroupPrice.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task TheProductListWithAnUnknownPriceGroup_IsNotFound()
    {
        var response = await fixture.CreateClient().GetAsync(
            "/api/v1/products?priceGroup=YOK-1", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
