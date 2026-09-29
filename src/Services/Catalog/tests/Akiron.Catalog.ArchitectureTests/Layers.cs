using System.Reflection;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Infrastructure.Persistence;

namespace Akiron.Catalog.ArchitectureTests;

/// <summary>
/// One anchor type per layer, so the rules name layers rather than assembly strings.
/// </summary>
internal static class Layers
{
    public const string DomainNamespace = "Akiron.Catalog.Domain";
    public const string ApplicationNamespace = "Akiron.Catalog.Application";
    public const string InfrastructureNamespace = "Akiron.Catalog.Infrastructure";
    public const string ApiNamespace = "Akiron.Catalog.Api";

    public static readonly Assembly Domain = typeof(Category).Assembly;
    public static readonly Assembly Application = typeof(ICatalogDbContext).Assembly;
    public static readonly Assembly Infrastructure = typeof(CatalogDbContext).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;

    public static readonly Assembly[] All = [Domain, Application, Infrastructure, Api];
}
