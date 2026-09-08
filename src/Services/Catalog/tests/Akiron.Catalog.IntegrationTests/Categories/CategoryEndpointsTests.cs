using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Akiron.Catalog.Application.Categories;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests.Categories;

/// <summary>
/// Covers the category endpoints against a real PostgreSQL instance. These run the
/// whole stack — routing, validation filter, handler, EF, value converters, the
/// exception handler — because that is where the interesting failures live.
/// </summary>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class CategoryEndpointsTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateCategory_ThenReadItBack_ReturnsWhatWasStored()
    {
        var client = fixture.CreateClient();

        var created = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Kış Lastikleri", slug = "kis-lastikleri" },
            TestContext.Current.CancellationToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull("a 201 must point at the resource it created");

        var body = await created.Content.ReadFromJsonAsync<CategoryResponse>(
            CatalogApiFixture.Json, TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Name.Should().Be("Kış Lastikleri");
        body.Slug.Should().Be("kis-lastikleri");

        var fetched = await client.GetFromJsonAsync<CategoryResponse>(
            $"/api/v1/categories/{body.Id.Value}", CatalogApiFixture.Json, TestContext.Current.CancellationToken);

        fetched.Should().BeEquivalentTo(body);
    }

    [Fact]
    public async Task CreateCategory_WithATakenSlug_IsRejectedAsAConflict()
    {
        var client = fixture.CreateClient();
        var request = new { name = "Kış Lastikleri", slug = "tekrar-eden-slug" };

        await client.PostAsJsonAsync("/api/v1/categories", request, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync("/api/v1/categories", request, TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        second.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("traceId").GetString()
            .Should().NotBeNullOrWhiteSpace("every problem response carries the id to look up in Jaeger");
    }

    /// <summary>
    /// The handler's duplicate check cannot see a row another transaction has not
    /// committed yet, so under concurrency the unique index is the only thing keeping
    /// slugs unique. Two simultaneous creates must therefore settle as one 201 and one
    /// 409 — never two 201s, and never a 500 leaking the database error.
    /// </summary>
    [Fact]
    public async Task CreateCategory_WhenTwoRequestsRace_LetsExactlyOneWin()
    {
        var request = new { name = "Yarış", slug = "yaris-slug" };

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ =>
                fixture.CreateClient().PostAsJsonAsync(
                    "/api/v1/categories", request, TestContext.Current.CancellationToken)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
    }

    [Fact]
    public async Task CreateCategory_WithAMalformedSlug_IsRejectedWithFieldErrors()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Kış Lastikleri", slug = "Bu Slug Olmaz" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("errors").TryGetProperty("slug", out _)
            .Should().BeTrue("validation failures name the field that was wrong");
    }

    [Fact]
    public async Task GetCategory_ThatDoesNotExist_IsNotFound()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/categories/{Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetCategory_WithAnIdThatIsNotAGuid_IsRejectedAsBadRequest()
    {
        var client = fixture.CreateClient();

        // CategoryId binds through IParsable. When TryParse says no, the framework
        // answers 400 before the handler runs — the id is malformed, not missing.
        var response = await client.GetAsync("/api/v1/categories/not-a-guid", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
