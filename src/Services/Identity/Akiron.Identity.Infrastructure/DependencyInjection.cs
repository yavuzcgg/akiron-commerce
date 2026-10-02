using Akiron.Identity.Application.Common;
using Akiron.Identity.Infrastructure.Persistence;
using Akiron.Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Akiron.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        string connectionString,
        SigningKeyStore keyStore,
        TokenSettings tokenSettings)
    {
        services.AddDbContext<IdentityServiceDbContext>(options =>
            IdentityServiceDbContextOptions.Apply(options, connectionString));

        services.AddScoped<IIdentityDbContext>(provider => provider.GetRequiredService<IdentityServiceDbContext>());

        // Singletons: the hasher and issuer are stateless, and the key is loaded once at
        // boot so a missing key fails the start rather than the first login.
        services.AddSingleton(keyStore);
        services.AddSingleton(tokenSettings);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        return services;
    }
}
