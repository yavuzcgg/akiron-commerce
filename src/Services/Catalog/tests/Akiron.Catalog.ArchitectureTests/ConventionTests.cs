using System.Reflection;
using System.Runtime.CompilerServices;
using Akiron.Catalog.Application.Common;
using AwesomeAssertions;

namespace Akiron.Catalog.ArchitectureTests;

/// <summary>
/// The structural rules from AGENTS.md and the Catalog guide that a compiler cannot see.
/// </summary>
public sealed class ConventionTests
{
    /// <summary>The technical-bucket folder names AGENTS.md forbids.</summary>
    private static readonly HashSet<string> ForbiddenFolders =
        ["Services", "Repositories", "Managers", "Helpers", "Dtos"];

    private static IEnumerable<Type> OwnTypes(params Assembly[] assemblies) =>
        assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));

    [Fact]
    public void NoCodeLivesInATechnicalBucketFolder()
    {
        var offenders = OwnTypes(Layers.All)
            .Where(type => type.Namespace is not null
                && type.Namespace.Split('.').Any(ForbiddenFolders.Contains))
            .Select(type => type.FullName);

        offenders.Should().BeEmpty("code is grouped by feature, not by technical role");
    }

    [Fact]
    public void EveryHandlerLivesInAFolderNamedAfterItsUseCase()
    {
        // Products/CreateProduct/CreateProductHandler — the folder is the use case, and
        // everything that use case needs (request, validator, handler) sits beside it.
        var misplaced = OwnTypes(Layers.Application)
            .Where(type => type.IsClass && type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type =>
            {
                var useCase = type.Name[..^"Handler".Length];
                return type.Namespace?.EndsWith($".{useCase}", StringComparison.Ordinal) != true;
            })
            .Select(type => type.FullName);

        misplaced.Should().BeEmpty();
    }

    [Fact]
    public void TheDbContextInterfaceGainsNoQueryMethods()
    {
        // Catalog AGENTS.md: a GetById here turns the interface into the generic
        // repository the constitution forbids. Sets and SaveChangesAsync, nothing more.
        var methods = typeof(ICatalogDbContext)
            .GetMethods()
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name);

        methods.Should().Equal("SaveChangesAsync");
    }

    [Fact]
    public void DomainEntitiesCannotBeChangedFromOutside()
    {
        // State changes go through methods that enforce the invariants. A public
        // setter would let a handler write a Money straight past its validation.
        var publicSetters = OwnTypes(Layers.Domain)
            .Where(type => type.IsClass)
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod is { IsPublic: true }
                && !property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}");

        publicSetters.Should().BeEmpty();
    }
}
