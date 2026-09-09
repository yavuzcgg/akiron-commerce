using System.Net;
using System.Net.Http.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Application.Products;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Products;

/// <summary>
/// Covers filtering and sorting on the product list. Filters are exact matches by
/// design: name search waits for Elasticsearch in Faz 3, because ILIKE gets Turkish
/// wrong and a search box that silently misses products is worse than none.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class ProductListTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<Guid> CreateCategoryAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/categories", new { name = "Lastikler", slug }, TestContext.Current.CancellationToken);

        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        return category!.Id.Value;
    }

    private static async Task CreateProductAsync(
        HttpClient client, Guid categoryId, string sku, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new { sku, name, description = (string?)null, categoryId, basePrice = price, currency = "TRY" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<PagedResponse<ProductResponse>> ListAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/api/v1/products{query}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>(
            fixture.Json, TestContext.Current.CancellationToken))!;
    }

    [Fact]
    public async Task ListProducts_FilteredByCategory_ReturnsOnlyThatCategory()
    {
        var client = fixture.CreateClient();
        var lastikler = await CreateCategoryAsync(client, "lastikler");
        var jantlar = await CreateCategoryAsync(client, "jantlar");

        await CreateProductAsync(client, lastikler, "LAST-1", "Michelin", 100m);
        await CreateProductAsync(client, lastikler, "LAST-2", "Pirelli", 200m);
        await CreateProductAsync(client, jantlar, "JANT-1", "Celik Jant", 300m);

        var page = await ListAsync(client, $"?categoryId={lastikler}");

        page.TotalCount.Should().Be(2, "the total reflects the filter, not the whole table");
        page.Items.Select(product => product.Sku).Should().BeEquivalentTo(["LAST-1", "LAST-2"]);
    }

    [Fact]
    public async Task ListProducts_FilteredByActiveFlag_SkipsTheDeactivatedOnes()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");
        await CreateProductAsync(client, categoryId, "ACTIVE-1", "Michelin", 100m);
        await CreateProductAsync(client, categoryId, "OFF-1", "Pirelli", 200m);

        var products = await ListAsync(client);
        var toDeactivate = products.Items.Single(product => product.Sku == "OFF-1");

        await client.PutAsJsonAsync(
            $"/api/v1/products/{toDeactivate.Id.Value}",
            new
            {
                name = toDeactivate.Name,
                description = (string?)null,
                categoryId,
                basePrice = toDeactivate.BasePrice.Amount,
                currency = toDeactivate.BasePrice.Currency,
                isActive = false,
            },
            TestContext.Current.CancellationToken);

        var active = await ListAsync(client, "?isActive=true");

        active.Items.Select(product => product.Sku).Should().BeEquivalentTo(["ACTIVE-1"]);
    }

    [Fact]
    public async Task ListProducts_FilteredBySku_FindsItWhateverTheCasing()
    {
        // The filter goes through the Sku value object, so it is normalised the same way
        // the stored value was. A lower-case query therefore finds the upper-case row.
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");
        await CreateProductAsync(client, categoryId, "MICH-205-55-R16", "Michelin", 100m);
        await CreateProductAsync(client, categoryId, "PIR-1", "Pirelli", 200m);

        var page = await ListAsync(client, "?sku=mich-205-55-r16");

        page.Items.Should().ContainSingle().Which.Sku.Should().Be("MICH-205-55-R16");
    }

    [Fact]
    public async Task ListProducts_SortedByPrice_OrdersOnTheAmountColumn()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");
        await CreateProductAsync(client, categoryId, "ORTA-1", "Orta", 250.00m);
        await CreateProductAsync(client, categoryId, "UCUZ-1", "Ucuz", 99.90m);
        await CreateProductAsync(client, categoryId, "PAHALI-1", "Pahali", 4990.50m);

        var page = await ListAsync(client, "?sort=basePrice&direction=asc");

        page.Items.Select(product => product.BasePrice.Amount)
            .Should().ContainInOrder(99.90m, 250.00m, 4990.50m);
    }

    [Fact]
    public async Task ListProducts_SortedByName_FollowsTheTurkishAlphabet()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");
        await CreateProductAsync(client, categoryId, "Z-1", "Zincir", 100m);
        await CreateProductAsync(client, categoryId, "C-1", "Çelik Jant", 200m);
        await CreateProductAsync(client, categoryId, "U-1", "Ürün", 300m);

        var page = await ListAsync(client, "?sort=name&direction=asc");

        page.Items.Select(product => product.Name)
            .Should().ContainInOrder("Çelik Jant", "Ürün", "Zincir");
    }

    [Fact]
    public async Task ListProducts_WithAMalformedSkuFilter_IsRejected()
    {
        // A SKU that could not exist is a truthful 400 rather than an empty result:
        // the caller asked something the catalogue cannot answer.
        var response = await fixture.CreateClient().GetAsync(
            "/api/v1/products?sku=not a sku", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
