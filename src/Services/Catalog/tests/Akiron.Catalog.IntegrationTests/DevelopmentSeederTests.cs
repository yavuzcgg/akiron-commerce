using System.Net.Http.Json;
using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Application.Pricing.QuotePrices;
using Akiron.Catalog.Application.Products;
using Akiron.Catalog.Infrastructure.Persistence;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Akiron.Catalog.IntegrationTests;

[Collection(CatalogApiCollectionDefinition.Name)]
public sealed class DevelopmentSeederTests(CatalogApiFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(fixture.ResetAsync());

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<bool> SeedAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await DevelopmentSeeder.SeedAsync(dbContext, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SeedingAnEmptyCatalogue_WritesIt_AndSeedingAgainChangesNothing()
    {
        (await SeedAsync()).Should().BeTrue();
        (await SeedAsync()).Should().BeFalse("a catalogue that already has data is left alone");

        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        (await dbContext.Categories.CountAsync(TestContext.Current.CancellationToken)).Should().Be(6);
        (await dbContext.Products.CountAsync(TestContext.Current.CancellationToken)).Should().Be(11);
        (await dbContext.PriceGroups.CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
    }

    [Fact]
    public async Task TheSeededCategoriesSortInTurkishAlphabetOrder()
    {
        await SeedAsync();

        var page = await fixture.CreateClient().GetFromJsonAsync<PagedResponse<CategoryResponse>>(
            "/api/v1/categories?sort=name", fixture.Json, TestContext.Current.CancellationToken);

        page!.Items.Select(category => category.Name).Should().Equal(
            "Çelik Jantlar",
            "Dört Mevsim Lastikleri",
            "İç Lastikler",
            "Kış Lastikleri",
            "Şanzıman Yağları",
            "Yaz Lastikleri");
    }

    [Fact]
    public async Task TheSeededDealerTierShowsBothPricingBases()
    {
        await SeedAsync();
        var client = fixture.CreateClient();

        var products = await client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            "/api/v1/products?pageSize=50", fixture.Json, TestContext.Current.CancellationToken);

        var primacy = products!.Items.Single(product => product.Sku == "MIC-PRIMACY4-205-55-16");
        var turanza = products.Items.Single(product => product.Sku == "BRI-TURANZA6-225-45-17");

        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/quote",
            new { priceGroupCode = "BAYI-A", productIds = new[] { primacy.Id.Value, turanza.Id.Value } },
            TestContext.Current.CancellationToken);

        var quotes = await response.Content.ReadFromJsonAsync<IReadOnlyList<QuotedPriceResponse>>(
            fixture.Json, TestContext.Current.CancellationToken);

        var primacyQuote = quotes!.Single(quote => quote.Sku == primacy.Sku);
        primacyQuote.Basis.Should().Be("AgreedPrice");
        primacyQuote.FinalPrice.Should().Be(3150.00m);

        var turanzaQuote = quotes!.Single(quote => quote.Sku == turanza.Sku);
        turanzaQuote.Basis.Should().Be("GroupDiscount");
        turanzaQuote.FinalPrice.Should().Be(4096.00m, "5120 less the 20% dealer discount");
    }
}
