using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Akiron.Catalog.IntegrationTests;

/// <summary>
/// Boots the real API host against a throwaway database.
/// </summary>
/// <remarks>
/// The connection string is injected as configuration rather than by swapping the
/// registered <c>DbContext</c>: the host then wires itself up exactly the way it does
/// in production, so this covers the real startup path instead of a rebuilt one.
/// The Testing environment also keeps startup migrations off — the fixture applies
/// them once, deliberately.
/// </remarks>
public sealed class CatalogApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CatalogDb", connectionString);
    }
}
