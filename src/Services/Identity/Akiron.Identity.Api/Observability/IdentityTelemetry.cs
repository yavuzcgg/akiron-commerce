using System.Reflection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Akiron.Identity.Api.Observability;

/// <summary>
/// OpenTelemetry wiring for the Identity service — Catalog's, renamed. The second copy
/// is the point: slice 2.2 replaces both with one shared registration.
/// </summary>
public static class IdentityTelemetry
{
    public const string ServiceName = "akiron-identity";

    public static string ServiceVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    public static IServiceCollection AddIdentityTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var exportsTelemetry = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: ServiceName, serviceVersion: ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (exportsTelemetry)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (exportsTelemetry)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
