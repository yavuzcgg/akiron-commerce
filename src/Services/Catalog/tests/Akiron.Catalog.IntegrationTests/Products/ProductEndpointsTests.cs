using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Products;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Products;

/// <summary>
/// Exercises the product endpoints against real PostgreSQL, which is where the parts
/// that cannot be unit tested live: the unique index, the restricted foreign key, and
/// the two columns Money is stored in.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class ProductEndpointsTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<Guid> CreateCategoryAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Lastikler", slug },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        return category!.Id.Value;
    }

    private static object NewProduct(Guid categoryId, string sku = "MICH-205-55-R16", decimal price = 4250.00m) =>
        new
        {
            sku,
            name = "Michelin Primacy 4",
            description = "Yaz lastigi",
            categoryId,
            basePrice = price,
            currency = "TRY",
        };

    [Fact]
    public async Task CreateProduct_ThenReadItBack_ReturnsWhatWasStored()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");

        var created = await client.PostAsJsonAsync(
            "/api/v1/products", NewProduct(categoryId), TestContext.Current.CancellationToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();

        var body = await created.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Sku.Should().Be("MICH-205-55-R16");
        body.BasePrice.Amount.Should().Be(4250.00m);
        body.BasePrice.Currency.Should().Be("TRY");
        body.IsActive.Should().BeTrue();

        var fetched = await client.GetFromJsonAsync<ProductResponse>(
            $"/api/v1/products/{body.Id.Value}", fixture.Json, TestContext.Current.CancellationToken);

        fetched.Should().BeEquivalentTo(body);
    }

    [Fact]
    public async Task CreateProduct_WithTheSameSkuInLowerCase_IsRejectedAsAConflict()
    {
        // The normalisation inside Sku is what makes this a conflict. Without it the
        // unique index would see two different strings and the catalogue would end up
        // holding the same product twice.
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");

        await client.PostAsJsonAsync(
            "/api/v1/products", NewProduct(categoryId, "ABC-1"), TestContext.Current.CancellationToken);

        var second = await client.PostAsJsonAsync(
            "/api/v1/products", NewProduct(categoryId, "abc-1"), TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProduct_WhenTwoRequestsRace_LetsExactlyOneWin()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");
        var product = NewProduct(categoryId, "YARIS-1");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ =>
                fixture.CreateClient().PostAsJsonAsync(
                    "/api/v1/products", product, TestContext.Current.CancellationToken)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
    }

    [Fact]
    public async Task CreateProduct_InACategoryThatDoesNotExist_IsNotFound()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/products", NewProduct(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithMoreThanTwoDecimalPlaces_IsRejected()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");

        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            NewProduct(categoryId, price: 10.005m),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("errors").TryGetProperty("basePrice", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProduct_ChangesTheEditableFieldsAndLeavesTheSku()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");

        var created = await client.PostAsJsonAsync(
            "/api/v1/products", NewProduct(categoryId), TestContext.Current.CancellationToken);
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        var updated = await client.PutAsJsonAsync(
            $"/api/v1/products/{product!.Id.Value}",
            new
            {
                name = "Michelin Primacy 5",
                description = (string?)null,
                categoryId,
                basePrice = 4990.50m,
                currency = "TRY",
                isActive = false,
            },
            TestContext.Current.CancellationToken);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await updated.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        body!.Name.Should().Be("Michelin Primacy 5");
        body.Description.Should().BeNull();
        body.BasePrice.Amount.Should().Be(4990.50m);
        body.IsActive.Should().BeFalse();
        body.Sku.Should().Be("MICH-205-55-R16", "the SKU is not part of the update contract");
        body.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteProduct_RemovesIt()
    {
        var client = fixture.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "lastikler");

        var created = await client.PostAsJsonAsync(
            "/api/v1/products", NewProduct(categoryId), TestContext.Current.CancellationToken);
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        var deleted = await client.DeleteAsync(
            $"/api/v1/products/{product!.Id.Value}", TestContext.Current.CancellationToken);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await client.GetAsync(
            $"/api/v1/products/{product.Id.Value}", TestContext.Current.CancellationToken);
        fetched.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProduct_ThatDoesNotExist_IsNotFound()
    {
        var response = await fixture.CreateClient().GetAsync(
            $"/api/v1/products/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
