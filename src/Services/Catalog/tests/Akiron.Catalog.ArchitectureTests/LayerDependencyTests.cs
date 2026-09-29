using AwesomeAssertions;
using NetArchTest.Rules;

namespace Akiron.Catalog.ArchitectureTests;

/// <summary>
/// The dependency direction from AGENTS.md, checked against the compiled IL.
/// </summary>
/// <remarks>
/// The project references already stop most of these at compile time — Domain has no
/// package to import EF Core from. What they do not stop is someone adding that
/// package reference. These rules fail on the type that took the dependency, which is
/// the review comment you would otherwise have to remember to write.
/// </remarks>
public sealed class LayerDependencyTests
{
    [Fact]
    public void TheDomainDependsOnNothingOutsideItself()
    {
        var result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                Layers.ApplicationNamespace,
                Layers.InfrastructureNamespace,
                Layers.ApiNamespace,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "FluentValidation")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void TheApplicationLayerKnowsNeitherTheDatabaseDriverNorHttp()
    {
        // EF Core is allowed here for DbSet<T> on ICatalogDbContext. Npgsql is not: the
        // one piece of raw SQL sits behind IPriceListWriter in Infrastructure. And a
        // handler that reaches for HttpContext can no longer be called from anywhere
        // but an endpoint.
        var result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                Layers.InfrastructureNamespace,
                Layers.ApiNamespace,
                "Npgsql",
                "Microsoft.AspNetCore")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void InfrastructureDoesNotReachUpIntoTheApi()
    {
        // Infrastructure does reference Application: it implements ICatalogDbContext
        // and IPriceListWriter, which is the whole point of declaring them there.
        var result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApiNamespace, "Microsoft.AspNetCore")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void NoLayerUsesMediatR()
    {
        // ADR-0006: MediatR is for Order, Payment and Inventory only. Catalog endpoints
        // call their handlers directly.
        var result = Types.InAssemblies(Layers.All)
            .ShouldNot()
            .HaveDependencyOn("MediatR")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }
}
