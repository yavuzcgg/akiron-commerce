using Akiron.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Applies pending migrations during startup.
/// </summary>
/// <remarks>
/// A development convenience only, gated on the environment by the caller.
/// Deployments run migrations as their own step: a service that migrates on boot
/// makes every replica race to change the same schema.
/// </remarks>
public static partial class MigrationRunner
{
    public static async Task ApplyAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await dbContext.Database.MigrateAsync();
            LogSchemaUpToDate(logger);
        }
        catch (Exception exception)
        {
            LogMigrationFailed(logger, exception);
            throw;
        }
    }

    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "Catalog database schema is up to date.")]
    private static partial void LogSchemaUpToDate(ILogger logger);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Critical,
        Message = "Database migration failed; the application will terminate.")]
    private static partial void LogMigrationFailed(ILogger logger, Exception exception);
}
