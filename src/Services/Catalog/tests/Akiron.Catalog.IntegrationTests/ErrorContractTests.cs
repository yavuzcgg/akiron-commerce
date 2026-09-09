using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests;

/// <summary>
/// Pins down the error contract from ADR-0018: every failure names itself with a stable
/// code and hands over the values a client needs to write its own message. These are
/// the assertions that would fail if someone renamed a code, which is exactly the
/// breaking change the ADR says needs deliberate thought.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class ErrorContractTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

    [Fact]
    public async Task NotFound_CarriesItsCodeAndTheIdThatWasNotFound()
    {
        var missingId = Guid.CreateVersion7();

        var response = await fixture.CreateClient().GetAsync(
            $"/api/v1/products/{missingId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.ProductNotFound);
        problem.GetProperty("params").GetProperty("productId").GetGuid().Should().Be(missingId);
        problem.GetProperty("detail").GetString()
            .Should().NotBeNullOrWhiteSpace("detail stays English for logs and bug reports");
    }

    [Fact]
    public async Task Conflict_CarriesItsCodeAndTheValueThatClashed()
    {
        var client = fixture.CreateClient();
        var request = new { name = "Lastikler", slug = "cakisan-slug" };

        await client.PostAsJsonAsync("/api/v1/categories", request, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync("/api/v1/categories", request, TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await ReadProblemAsync(second);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.CategorySlugConflict);
        problem.GetProperty("params").GetProperty("slug").GetString().Should().Be("cakisan-slug");
    }

    [Fact]
    public async Task ValidationFailure_CarriesACodeForEveryRejectedField()
    {
        var response = await fixture.CreateClient().PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "", slug = "Bu Slug Olmaz" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.ValidationFailed);

        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("slug", out var slugErrors).Should().BeTrue();
        errors.TryGetProperty("name", out _).Should().BeTrue();

        var firstSlugError = slugErrors.EnumerateArray().First();
        firstSlugError.GetProperty("code").GetString().Should().NotBeNullOrWhiteSpace();
        firstSlugError.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DeletingACategoryThatStillHasProducts_CarriesTheHasProductsCode()
    {
        var client = fixture.CreateClient();
        var created = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Dolu", slug = "dolu-kategori" },
            TestContext.Current.CancellationToken);
        var category = await created.Content.ReadFromJsonAsync<CategoryResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        await client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                sku = "DOLU-1",
                name = "Michelin",
                description = (string?)null,
                categoryId = category!.Id.Value,
                basePrice = 100.00m,
                currency = "TRY",
            },
            TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync(
            $"/api/v1/categories/{category.Id.Value}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.CategoryHasProducts);
    }

    [Fact]
    public async Task EveryProblemResponse_CarriesTheTraceIdToLookUpInJaeger()
    {
        var response = await fixture.CreateClient().GetAsync(
            $"/api/v1/categories/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
