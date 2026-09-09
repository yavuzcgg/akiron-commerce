using System.Net;
using System.Net.Http.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Products;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Categories;

/// <summary>
/// Covers renaming and deleting a category. The delete cases are the interesting ones:
/// they prove the restricted foreign key, not the handler, is what stops a category
/// disappearing out from under its products.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class CategoryLifecycleTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Lastikler", slug },
            TestContext.Current.CancellationToken);

        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(
            fixture.Json, TestContext.Current.CancellationToken))!;
    }

    [Fact]
    public async Task UpdateCategory_RenamesItAndKeepsTheSlug()
    {
        var client = fixture.CreateClient();
        var category = await CreateCategoryAsync(client, "lastikler");

        var updated = await client.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id.Value}",
            new { name = "Kis Lastikleri" },
            TestContext.Current.CancellationToken);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await updated.Content.ReadFromJsonAsync<CategoryResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        body!.Name.Should().Be("Kis Lastikleri");
        body.Slug.Should().Be("lastikler", "the slug is part of public URLs, so renaming must not move it");
        body.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateCategory_ThatDoesNotExist_IsNotFound()
    {
        var response = await fixture.CreateClient().PutAsJsonAsync(
            $"/api/v1/categories/{Guid.CreateVersion7()}",
            new { name = "Yeni ad" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_WithNoProducts_RemovesIt()
    {
        var client = fixture.CreateClient();
        var category = await CreateCategoryAsync(client, "bos-kategori");

        var deleted = await client.DeleteAsync(
            $"/api/v1/categories/{category.Id.Value}", TestContext.Current.CancellationToken);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await client.GetAsync(
            $"/api/v1/categories/{category.Id.Value}", TestContext.Current.CancellationToken);
        fetched.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ThatStillHasProducts_IsRejectedByTheForeignKey()
    {
        // The handler does not look for products before deleting. The foreign key is
        // configured to restrict, so PostgreSQL refuses the delete and the exception
        // handler turns error 23503 into a 409 the caller can act on.
        var client = fixture.CreateClient();
        var category = await CreateCategoryAsync(client, "dolu-kategori");

        var productCreated = await client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                sku = "DOLU-1",
                name = "Michelin Primacy 4",
                description = (string?)null,
                categoryId = category.Id.Value,
                basePrice = 100.00m,
                currency = "TRY",
            },
            TestContext.Current.CancellationToken);
        productCreated.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleted = await client.DeleteAsync(
            $"/api/v1/categories/{category.Id.Value}", TestContext.Current.CancellationToken);

        deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        deleted.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        // And the category is still there, because the whole delete was refused.
        var fetched = await client.GetAsync(
            $"/api/v1/categories/{category.Id.Value}", TestContext.Current.CancellationToken);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteCategory_AfterItsProductsAreGone_Succeeds()
    {
        var client = fixture.CreateClient();
        var category = await CreateCategoryAsync(client, "temizlenen-kategori");

        var productCreated = await client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                sku = "TEMIZ-1",
                name = "Michelin Primacy 4",
                description = (string?)null,
                categoryId = category.Id.Value,
                basePrice = 100.00m,
                currency = "TRY",
            },
            TestContext.Current.CancellationToken);

        var product = await productCreated.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        await client.DeleteAsync($"/api/v1/products/{product!.Id.Value}", TestContext.Current.CancellationToken);

        var deleted = await client.DeleteAsync(
            $"/api/v1/categories/{category.Id.Value}", TestContext.Current.CancellationToken);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
