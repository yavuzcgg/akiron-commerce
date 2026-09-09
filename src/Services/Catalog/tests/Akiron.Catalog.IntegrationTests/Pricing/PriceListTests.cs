using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Application.Pricing;
using Akiron.Catalog.Application.Products;
using Akiron.Catalog.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Pricing;

/// <summary>
/// Covers price groups and the prices inside them. The interesting cases are the ones
/// only a real database can answer: the composite key, the two foreign keys that behave
/// differently on delete, and whether an upsert survives two callers arriving at once.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class PriceListTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<Guid> CreateProductAsync(HttpClient client, string sku = "MICH-1")
    {
        var categoryResponse = await client.PostAsJsonAsync(
            "/api/v1/categories",
            // The whole GUID, not a prefix of it: version 7 puts a timestamp in the
            // leading bits, so two calls in the same millisecond share their first
            // characters and would collide on the unique slug index.
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
                basePrice = 4250.00m,
                currency = "TRY",
            },
            TestContext.Current.CancellationToken);

        productResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        return product!.Id.Value;
    }

    private static async Task<HttpResponseMessage> CreatePriceGroupAsync(
        HttpClient client, string code = "BAYI-A", string currency = "TRY", decimal discount = 10m) =>
        await client.PostAsJsonAsync(
            "/api/v1/price-groups",
            new { code, name = "Bayi A", currency, discountPercentage = discount },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task CreatePriceGroup_ThenReadItBack_ReturnsWhatWasStored()
    {
        var client = fixture.CreateClient();

        var created = await CreatePriceGroupAsync(client);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await created.Content.ReadFromJsonAsync<PriceGroupResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        body!.Code.Should().Be("BAYI-A");
        body.Currency.Should().Be("TRY");
        body.DiscountPercentage.Should().Be(10m);

        var fetched = await client.GetFromJsonAsync<PriceGroupResponse>(
            "/api/v1/price-groups/BAYI-A", fixture.Json, TestContext.Current.CancellationToken);

        fetched.Should().BeEquivalentTo(body);
    }

    /// <summary>
    /// Every typed id is written as a bare uuid string. This is asserted on the raw JSON
    /// rather than through the response type because a missing converter still
    /// deserialises fine — it only shows up on the wire, as an id wrapped in an object.
    /// </summary>
    [Fact]
    public async Task PriceGroupResponse_WritesItsIdAsAPlainString()
    {
        var client = fixture.CreateClient();

        var created = await CreatePriceGroupAsync(client);
        var json = await created.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        json.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
        json.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreatePriceGroup_WithTheSameCodeInLowerCase_IsRejectedAsAConflict()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client, "BAYI-A");

        var second = await CreatePriceGroupAsync(client, "bayi-a");

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.PriceGroupCodeConflict);
    }

    [Fact]
    public async Task UpdatePriceGroup_ChangesTheDiscountButNotTheCurrency()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client, "BAYI-A", "TRY", 10m);

        var updated = await client.PutAsJsonAsync(
            "/api/v1/price-groups/BAYI-A",
            new { name = "Bayi A - guncel", discountPercentage = 17.50m },
            TestContext.Current.CancellationToken);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await updated.Content.ReadFromJsonAsync<PriceGroupResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        body!.DiscountPercentage.Should().Be(17.50m);
        body.Name.Should().Be("Bayi A - guncel");
        body.Currency.Should().Be("TRY", "the currency is not part of the update contract");
        body.Code.Should().Be("BAYI-A");
    }

    [Fact]
    public async Task UpsertPrice_CreatesThenReplaces()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client);
        var productId = await CreateProductAsync(client);

        var first = await client.PutAsJsonAsync(
            $"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 3800.00m },
            TestContext.Current.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PutAsJsonAsync(
            $"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 3700.00m },
            TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.OK, "the second call replaces rather than creating");

        var body = await second.Content.ReadFromJsonAsync<PriceListEntryResponse>(
            fixture.Json, TestContext.Current.CancellationToken);

        body!.Amount.Should().Be(3700.00m);
        body.Currency.Should().Be("TRY", "the currency is inherited from the group, never sent by the caller");
    }

    /// <summary>
    /// PUT is defined to be idempotent: sending it twice must leave the same state and
    /// answer successfully both times. A read-then-write upsert cannot promise that —
    /// two callers can both find nothing and then both try to insert, and the composite
    /// key rejects the loser. This test is what turns that into a visible failure.
    /// </summary>
    [Fact]
    public async Task UpsertPrice_WhenTwoRequestsRace_SucceedsForBoth()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client);
        var productId = await CreateProductAsync(client);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ =>
                fixture.CreateClient().PutAsJsonAsync(
                    $"/api/v1/price-groups/BAYI-A/prices/{productId}",
                    new { amount = 3800.00m },
                    TestContext.Current.CancellationToken)));

        responses.Should().AllSatisfy(response =>
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK));
    }

    [Fact]
    public async Task UpsertPrice_ForAProductThatDoesNotExist_IsNotFound()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/price-groups/BAYI-A/prices/{Guid.CreateVersion7()}",
            new { amount = 100.00m },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpsertPrice_InAGroupThatDoesNotExist_IsNotFound()
    {
        var client = fixture.CreateClient();
        var productId = await CreateProductAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/price-groups/YOK-1/prices/{productId}",
            new { amount = 100.00m },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.PriceGroupNotFound);
    }

    [Fact]
    public async Task ListPrices_ReturnsThePagedEntriesOfThatGroupOnly()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client, "BAYI-A");
        await CreatePriceGroupAsync(client, "TOPTAN");

        var productId = await CreateProductAsync(client, "MICH-1");
        var otherProductId = await CreateProductAsync(client, "PIR-1");

        await client.PutAsJsonAsync($"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 3800.00m }, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"/api/v1/price-groups/BAYI-A/prices/{otherProductId}",
            new { amount = 3900.00m }, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"/api/v1/price-groups/TOPTAN/prices/{productId}",
            new { amount = 3500.00m }, TestContext.Current.CancellationToken);

        var page = await client.GetFromJsonAsync<PagedResponse<PriceListEntryResponse>>(
            "/api/v1/price-groups/BAYI-A/prices", fixture.Json, TestContext.Current.CancellationToken);

        page!.TotalCount.Should().Be(2);
        page.Items.Select(entry => entry.Amount).Should().BeEquivalentTo([3800.00m, 3900.00m]);
    }

    [Fact]
    public async Task DeletePrice_RemovesOnlyThatEntry()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client);
        var productId = await CreateProductAsync(client);

        await client.PutAsJsonAsync($"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 3800.00m }, TestContext.Current.CancellationToken);

        var deleted = await client.DeleteAsync(
            $"/api/v1/price-groups/BAYI-A/prices/{productId}", TestContext.Current.CancellationToken);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var page = await client.GetFromJsonAsync<PagedResponse<PriceListEntryResponse>>(
            "/api/v1/price-groups/BAYI-A/prices", fixture.Json, TestContext.Current.CancellationToken);
        page!.TotalCount.Should().Be(0);

        var groupStillThere = await client.GetAsync(
            "/api/v1/price-groups/BAYI-A", TestContext.Current.CancellationToken);
        groupStillThere.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePriceGroup_ThatStillHoldsPrices_IsRejectedByTheForeignKey()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client);
        var productId = await CreateProductAsync(client);

        await client.PutAsJsonAsync($"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 3800.00m }, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync(
            "/api/v1/price-groups/BAYI-A", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("code").GetString().Should().Be(CatalogErrorCodes.PriceGroupHasPrices);
    }

    /// <summary>
    /// The mirror image of the previous test, and the reason the two foreign keys are
    /// configured differently: a price for a deleted product has nothing left to mean, so
    /// it goes with it, while a price group full of agreed prices must be emptied on purpose.
    /// </summary>
    [Fact]
    public async Task DeletingAProduct_TakesItsPricesWithIt()
    {
        var client = fixture.CreateClient();
        await CreatePriceGroupAsync(client);
        var productId = await CreateProductAsync(client);

        await client.PutAsJsonAsync($"/api/v1/price-groups/BAYI-A/prices/{productId}",
            new { amount = 3800.00m }, TestContext.Current.CancellationToken);

        var deleted = await client.DeleteAsync(
            $"/api/v1/products/{productId}", TestContext.Current.CancellationToken);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var page = await client.GetFromJsonAsync<PagedResponse<PriceListEntryResponse>>(
            "/api/v1/price-groups/BAYI-A/prices", fixture.Json, TestContext.Current.CancellationToken);

        page!.TotalCount.Should().Be(0, "the price went with the product it described");
    }
}
