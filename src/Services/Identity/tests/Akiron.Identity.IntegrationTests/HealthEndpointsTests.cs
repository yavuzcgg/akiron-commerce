using System.Net;
using AwesomeAssertions;

namespace Akiron.Identity.IntegrationTests;

[Collection(IdentityApiCollectionDefinition.Name)]
public sealed class HealthEndpointsTests(IdentityApiFixture fixture)
{
    [Fact]
    public async Task Readiness_ReportsHealthyOnceTheSchemaExists()
    {
        var response = await fixture.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
