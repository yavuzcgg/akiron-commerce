using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Akiron.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            CatalogDbContextOptions.Apply(options, connectionString));

        // Handlers depend on the interface; only this line knows which class implements it.
        services.AddScoped<ICatalogDbContext>(provider => provider.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IPriceListWriter, PriceListWriter>();

        return services;
    }
}
