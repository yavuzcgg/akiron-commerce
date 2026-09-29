using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using Akiron.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Fills an empty local database with a small catalogue, so Scalar has something to
/// answer with on the first F5.
/// </summary>
/// <remarks>
/// Deliberately not <c>HasData</c>: seed rows written into a migration travel to every
/// environment the migration runs in, production included. This runs only in
/// Development, only against an empty catalogue, and goes through the domain factories
/// like any other write — seed data that bypassed validation would be the one place an
/// invalid product could exist.
/// The category names are chosen to show the Turkish collation: Ç, İ and Ş sort in
/// their alphabet places, not after Z.
/// </remarks>
public static partial class DevelopmentSeeder
{
    public static async Task ApplyAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        // Our own category, not ILogger<WebApplication>: that one is filed under
        // Microsoft.AspNetCore, which appsettings turns down to Warning, so every
        // Information line written through it was silently dropped.
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DevelopmentSeeder).FullName!);
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        if (await SeedAsync(dbContext, CancellationToken.None))
        {
            LogSeeded(logger);
        }
    }

    /// <returns><c>true</c> when rows were written; <c>false</c> when the catalogue already had data.</returns>
    public static async Task<bool> SeedAsync(CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        // Any existing category means someone has been using this database. Seeding on
        // top of their data would mix the two, so an empty catalogue is the only trigger.
        if (await dbContext.Categories.AnyAsync(cancellationToken))
        {
            return false;
        }

        var summer = Category.Create("Yaz Lastikleri", Slug.Create("yaz-lastikleri"));
        var winter = Category.Create("Kış Lastikleri", Slug.Create("kis-lastikleri"));
        var allSeason = Category.Create("Dört Mevsim Lastikleri", Slug.Create("dort-mevsim-lastikleri"));
        var innerTubes = Category.Create("İç Lastikler", Slug.Create("ic-lastikler"));
        var wheels = Category.Create("Çelik Jantlar", Slug.Create("celik-jantlar"));
        var oils = Category.Create("Şanzıman Yağları", Slug.Create("sanziman-yaglari"));

        dbContext.Categories.AddRange(summer, winter, allSeason, innerTubes, wheels, oils);

        var primacy = NewProduct("MIC-PRIMACY4-205-55-16", "Michelin Primacy 4 205/55 R16", summer, 4250.00m);
        var turanza = NewProduct("BRI-TURANZA6-225-45-17", "Bridgestone Turanza 6 225/45 R17", summer, 5120.00m);
        var sportContact = NewProduct("CON-SC7-245-40-18", "Continental SportContact 7 245/40 R18", summer, 7890.50m);
        var alpin = NewProduct("MIC-ALPIN6-205-55-16", "Michelin Alpin 6 205/55 R16", winter, 4680.00m);
        var blizzak = NewProduct("BRI-BLIZZAK6-225-45-17", "Bridgestone Blizzak 6 225/45 R17", winter, 5540.00m);
        var vector = NewProduct("GDY-VECTOR4S-205-55-16", "Goodyear Vector 4Seasons Gen-3 205/55 R16", allSeason, 4390.00m);
        var crossClimate = NewProduct("MIC-CROSSCLIMATE2-225-45-17", "Michelin CrossClimate 2 225/45 R17", allSeason, 5975.00m);
        var tube = NewProduct("TUBE-16-STD", "Standart İç Lastik 16\"", innerTubes, 385.00m);
        var wheel16 = NewProduct("JANT-CELIK-16-5X112", "Çelik Jant 16\" 5x112", wheels, 2150.00m);
        var wheel17 = NewProduct("JANT-CELIK-17-5X112", "Çelik Jant 17\" 5x112", wheels, 2640.00m);
        var atf = NewProduct("YAG-ATF-DEXRON6-1L", "ATF Dexron VI Şanzıman Yağı 1 L", oils, 689.90m);

        dbContext.Products.AddRange(
            primacy, turanza, sportContact, alpin, blizzak, vector, crossClimate, tube, wheel16, wheel17, atf);

        // The three tiers from a typical Logo setup: the dealer pays least, the retail
        // group pays the list price.
        var dealer = PriceGroup.Create(PriceGroupCode.Create("BAYI-A"), "A Sınıfı Bayi", Currency.TRY, DiscountPercentage.Create(20m));
        var wholesale = PriceGroup.Create(PriceGroupCode.Create("TOPTAN"), "Toptan", Currency.TRY, DiscountPercentage.Create(12m));
        var retail = PriceGroup.Create(PriceGroupCode.Create("PERAKENDE"), "Perakende", Currency.TRY, DiscountPercentage.Create(0m));

        dbContext.PriceGroups.AddRange(dealer, wholesale, retail);

        // Two negotiated deals, so a quote shows both bases side by side: Primacy at an
        // agreed price below the 20% tier (3400), Alpin above it.
        dbContext.PriceListEntries.AddRange(
            PriceListEntry.Create(dealer, primacy.Id, Money.Create(3150.00m, Currency.TRY)),
            PriceListEntry.Create(dealer, alpin.Id, Money.Create(3900.00m, Currency.TRY)));

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static Product NewProduct(string sku, string name, Category category, decimal listPrice) =>
        Product.Create(Sku.Create(sku), name, description: null, category.Id, Money.Create(listPrice, Currency.TRY));

    [LoggerMessage(EventId = 1110, Level = LogLevel.Information, Message = "Seeded the empty catalogue with development data.")]
    private static partial void LogSeeded(ILogger logger);
}
