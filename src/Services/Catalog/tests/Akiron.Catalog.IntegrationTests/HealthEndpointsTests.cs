using System.Net;
using AwesomeAssertions;

namespace Akiron.Catalog.IntegrationTests;

[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class HealthEndpointsTests(CatalogApiFixture fixture)
{
    [Fact]
    public async Task Liveness_AnswersWithoutTouchingTheDatabase()
    {
        var response = await fixture.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_ReportsHealthyWhenTheDatabaseIsReachable()
    {
        var response = await fixture.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Healthy");
    }
}
