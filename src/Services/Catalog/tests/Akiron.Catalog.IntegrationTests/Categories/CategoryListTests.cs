using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Categories;

/// <summary>
/// Covers the list endpoint: the paging envelope, its limits, and the sort order.
/// The Turkish sorting case is the one that matters most — it is wrong by default and
/// only right because the column carries an explicit collation.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class CategoryListTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task CreateCategoryAsync(HttpClient client, string name, string slug)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/categories", new { name, slug }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<PagedResponse<CategoryResponse>> ListAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/api/v1/categories{query}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<PagedResponse<CategoryResponse>>(
            fixture.Json, TestContext.Current.CancellationToken))!;
    }

    [Fact]
    public async Task ListCategories_ByDefault_ReturnsTheFirstPageNewestFirst()
    {
        var client = fixture.CreateClient();
        await CreateCategoryAsync(client, "Birinci", "birinci");
        await CreateCategoryAsync(client, "Ikinci", "ikinci");
        await CreateCategoryAsync(client, "Ucuncu", "ucuncu");

        var page = await ListAsync(client);

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(PagingRequest.DefaultPageSize);
        page.TotalCount.Should().Be(3);
        // The collection overload, not the params one: passing the reason as a loose
        // argument would make it a fourth expected item.
        page.Items.Select(category => category.Name)
            .Should().ContainInOrder(
                ["Ucuncu", "Ikinci", "Birinci"],
                "newest first is the default for editable lists");
    }

    [Fact]
    public async Task ListCategories_SortedByName_FollowsTheTurkishAlphabet()
    {
        // Under the database default collation this comes back as Zincir, Celik, Urunler:
        // the Turkish letters sort after z. It is only correct because the name column
        // carries COLLATE "tr-TR-x-icu".
        var client = fixture.CreateClient();
        await CreateCategoryAsync(client, "Zincir", "zincir");
        await CreateCategoryAsync(client, "Çelik Jant", "celik-jant");
        await CreateCategoryAsync(client, "Ürünler", "urunler");

        var page = await ListAsync(client, "?sort=name&direction=asc");

        page.Items.Select(category => category.Name)
            .Should().ContainInOrder("Çelik Jant", "Ürünler", "Zincir");
    }

    [Fact]
    public async Task ListCategories_OnASecondPage_ReturnsTheRemainderAndTheRealTotal()
    {
        var client = fixture.CreateClient();
        for (var index = 0; index < 5; index++)
        {
            await CreateCategoryAsync(client, $"Kategori {index}", $"kategori-{index}");
        }

        var page = await ListAsync(client, "?page=2&pageSize=2");

        page.Page.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(5, "the total counts every row, not the ones on this page");
    }

    [Fact]
    public async Task ListCategories_BeyondTheLastPage_IsEmptyButStillReportsTheTotal()
    {
        var client = fixture.CreateClient();
        await CreateCategoryAsync(client, "Tek", "tek");

        var page = await ListAsync(client, "?page=99&pageSize=20");

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(1);
    }

    [Theory]
    [InlineData("?pageSize=101", "pageSize", CatalogErrorCodes.PageSizeTooLarge)]
    [InlineData("?pageSize=0", "pageSize", CatalogErrorCodes.PageSizeTooLarge)]
    [InlineData("?page=0", "page", CatalogErrorCodes.PageOutOfRange)]
    [InlineData("?sort=colour", "sort", CatalogErrorCodes.SortNotSupported)]
    [InlineData("?direction=sideways", "direction", CatalogErrorCodes.SortNotSupported)]
    public async Task ListCategories_WithUnacceptableParameters_IsRejectedWithItsCode(
        string query, string expectedField, string expectedCode)
    {
        // Oversized pages are refused rather than clamped: quietly returning a different
        // page size than asked for is how a client ends up believing it has read
        // everything when it has not.
        var response = await fixture.CreateClient().GetAsync(
            $"/api/v1/categories{query}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);

        // The field key is part of the contract too: a client keys its messages on it, so
        // "pageSize.Value" leaking out of the validator expression would be a bug.
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty(expectedField, out var fieldErrors)
            .Should().BeTrue($"the failure should be reported under '{expectedField}'");

        fieldErrors.EnumerateArray()
            .Select(error => error.GetProperty("code").GetString())
            .Should().Contain(expectedCode);
    }
}
