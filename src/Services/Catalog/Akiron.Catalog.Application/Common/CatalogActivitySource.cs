using System.Diagnostics;

namespace Akiron.Catalog.Application.Common;

/// <summary>
/// Spans this service creates itself, on top of the ones the framework produces.
/// </summary>
/// <remarks>
/// Price resolution earns a span because it is the one piece of work here that is neither
/// an HTTP request nor a database call, so nothing else would record it. When a dealer
/// asks why a price came out the way it did, the trace shows which group and how many
/// markup levels were involved without anyone reading the code.
/// </remarks>
public static class CatalogActivitySource
{
    public const string Name = "Akiron.Catalog";

    public const string ResolvePrices = "catalog.price.resolve";

    public static ActivitySource Instance { get; } = new(Name);
}
