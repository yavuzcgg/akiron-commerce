using System.Reflection;

namespace Akiron.Catalog.Api.Observability;

/// <summary>Identity this service reports to the telemetry pipeline.</summary>
public static class CatalogTelemetry
{
    public const string ServiceName = "akiron-catalog";

    public static string ServiceVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
}
