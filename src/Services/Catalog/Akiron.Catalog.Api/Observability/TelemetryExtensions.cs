using Akiron.Catalog.Api.Observability;
using Akiron.Catalog.Application.Common;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OpenTelemetry wiring for the Catalog service.
/// </summary>
/// <remarks>
/// This lives inside the service rather than in a shared ServiceDefaults project:
/// AGENTS.md only allows a shared abstraction once two services need the same code,
/// and Catalog is currently the only one. It moves out when Identity arrives.
/// The OTLP endpoint comes from OTEL_EXPORTER_OTLP_ENDPOINT (Jaeger on 4317 locally;
/// the Aspire dashboard on 4319 is the alternate pane).
/// </remarks>
public static class TelemetryExtensions
{
    public static IServiceCollection AddCatalogTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // No endpoint configured means nothing is listening — tests and one-off runs
        // should not spend time retrying an exporter nobody asked for. Telemetry is
        // still collected in-process; it just is not shipped anywhere.
        var exportsTelemetry = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: CatalogTelemetry.ServiceName,
                serviceVersion: CatalogTelemetry.ServiceVersion))
            .WithTracing(tracing => tracing
                // Health probes fire constantly and say nothing about the domain;
                // tracing them would bury the requests that matter.
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !IsHealthProbe(context.Request.Path))
                .AddHttpClientInstrumentation()
                // EF Core has no stable instrumentation package; Npgsql emits spans itself.
                .AddNpgsql()
                // Our own spans; without this the price resolution span is created and dropped.
                .AddSource(CatalogActivitySource.Name)
                .AddOtlpExporterWhen(exportsTelemetry))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporterWhen(exportsTelemetry));

        return services;
    }

    private static bool IsHealthProbe(PathString path) => path.StartsWithSegments("/health");

    private static TracerProviderBuilder AddOtlpExporterWhen(this TracerProviderBuilder builder, bool enabled) =>
        enabled ? builder.AddOtlpExporter() : builder;

    private static MeterProviderBuilder AddOtlpExporterWhen(this MeterProviderBuilder builder, bool enabled) =>
        enabled ? builder.AddOtlpExporter() : builder;
}
