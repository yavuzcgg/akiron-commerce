using System.Text.Json;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Categories;
using AwesomeAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Akiron.Catalog.IntegrationTests;

/// <summary>
/// The price of registering everything by hand in Program.cs, paid back in tests.
/// </summary>
/// <remarks>
/// No assembly scanning means a new handler that nobody registered compiles fine and
/// fails on its first request. These tests walk the assemblies the way a scanner would
/// — but only to check the explicit list, never to build it.
/// </remarks>
[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class WiringTests(CatalogApiFixture fixture)
{
    private static readonly Type[] ApplicationTypes = typeof(ICatalogDbContext).Assembly.GetTypes();

    [Fact]
    public void EveryHandlerIsRegistered()
    {
        using var scope = fixture.Services.CreateScope();

        var unregistered = ApplicationTypes
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type => scope.ServiceProvider.GetService(type) is null)
            .Select(type => type.Name);

        unregistered.Should().BeEmpty("a handler missing from Program.cs fails on its first request");
    }

    [Fact]
    public void EveryValidatorIsRegistered()
    {
        // A missing validator is quieter than a missing handler: the endpoint filter finds
        // nothing to run and lets the request through unvalidated.
        using var scope = fixture.Services.CreateScope();

        // Matched on the interface, not the base class: the paging validators derive from
        // PagingRequestValidator<T>, and a base-class filter would skip them unnoticed.
        // That open generic base is itself excluded; only closed validators get registered.
        var validators = ApplicationTypes
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Select(type => (Type: type, Contract: type.GetInterfaces().SingleOrDefault(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IValidator<>))))
            .Where(candidate => candidate.Contract is not null)
            .ToList();

        validators.Should().NotBeEmpty();

        var unregistered = validators
            .Where(candidate => scope.ServiceProvider.GetService(candidate.Contract!)?.GetType() != candidate.Type)
            .Select(candidate => candidate.Type.Name);

        unregistered.Should().BeEmpty("an unregistered validator lets bad input through silently");
    }

    [Fact]
    public void EveryTypedIdTravelsAsAPlainGuid()
    {
        // The PriceGroupId bug from slice 1.4, turned into a rule: an id without a JSON
        // converter serialises as {"value": "..."} and breaks the wire contract.
        var typedIds = typeof(CategoryId).Assembly.GetTypes()
            .Where(type => type is { IsValueType: true, IsEnum: false }
                && type.Name.EndsWith("Id", StringComparison.Ordinal)
                && type.GetProperty("Value")?.PropertyType == typeof(Guid))
            .ToList();

        typedIds.Should().NotBeEmpty();

        var guid = Guid.CreateVersion7();

        foreach (var idType in typedIds)
        {
            var id = Activator.CreateInstance(idType, guid);

            JsonSerializer.Serialize(id, idType, fixture.Json)
                .Should().Be($"\"{guid}\"", $"{idType.Name} should serialise as a bare uuid string");
        }
    }
}
