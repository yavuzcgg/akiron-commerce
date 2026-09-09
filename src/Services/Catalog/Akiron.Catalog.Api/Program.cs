using System.Diagnostics;
using Akiron.Catalog.Api.Categories;
using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Api.Pricing;
using Akiron.Catalog.Api.Products;
using Akiron.Catalog.Application.Categories.CreateCategory;
using Akiron.Catalog.Application.Categories.DeleteCategory;
using Akiron.Catalog.Application.Categories.GetCategory;
using Akiron.Catalog.Application.Categories.ListCategories;
using Akiron.Catalog.Application.Categories.UpdateCategory;
using Akiron.Catalog.Application.Products.CreateProduct;
using Akiron.Catalog.Application.Products.DeleteProduct;
using Akiron.Catalog.Application.Products.GetProduct;
using Akiron.Catalog.Application.Products.ListProducts;
using Akiron.Catalog.Application.Pricing.CreatePriceGroup;
using Akiron.Catalog.Application.Pricing.DeletePrice;
using Akiron.Catalog.Application.Pricing.DeletePriceGroup;
using Akiron.Catalog.Application.Pricing.GetPriceGroup;
using Akiron.Catalog.Application.Pricing.ListPriceGroups;
using Akiron.Catalog.Application.Pricing.ListPrices;
using Akiron.Catalog.Application.Pricing.UpdatePriceGroup;
using Akiron.Catalog.Application.Pricing.UpsertPrice;
using Akiron.Catalog.Application.Products.UpdateProduct;
using Akiron.Catalog.Infrastructure;
using Akiron.Catalog.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Fail fast. A missing connection string must stop the process at boot rather than
// surface as a 500 on the first request that touches the database.
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("ConnectionStrings:CatalogDb is not configured.");

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    // Structured JSON on stdout: the container runtime collects it, and the fields
    // survive into whatever ships the logs later.
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddCatalogTelemetry(builder.Configuration);
builder.Services.AddCatalogInfrastructure(connectionString);

// Handlers and validators are registered one by one on purpose. No assembly scanning
// anywhere in this service: the set of wired-up types stays greppable.
builder.Services.AddScoped<CreateCategoryHandler>();
builder.Services.AddScoped<GetCategoryHandler>();
builder.Services.AddScoped<ListCategoriesHandler>();
builder.Services.AddScoped<UpdateCategoryHandler>();
builder.Services.AddScoped<DeleteCategoryHandler>();
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetProductHandler>();
builder.Services.AddScoped<ListProductsHandler>();
builder.Services.AddScoped<CreatePriceGroupHandler>();
builder.Services.AddScoped<GetPriceGroupHandler>();
builder.Services.AddScoped<ListPriceGroupsHandler>();
builder.Services.AddScoped<UpdatePriceGroupHandler>();
builder.Services.AddScoped<DeletePriceGroupHandler>();
builder.Services.AddScoped<UpsertPriceHandler>();
builder.Services.AddScoped<ListPricesHandler>();
builder.Services.AddScoped<DeletePriceHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateCategoryRequest>, UpdateCategoryRequestValidator>();
builder.Services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();
builder.Services.AddScoped<IValidator<ListCategoriesRequest>, ListCategoriesRequestValidator>();
builder.Services.AddScoped<IValidator<ListProductsRequest>, ListProductsRequestValidator>();
builder.Services.AddScoped<IValidator<CreatePriceGroupRequest>, CreatePriceGroupRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePriceGroupRequest>, UpdatePriceGroupRequestValidator>();
builder.Services.AddScoped<IValidator<UpsertPriceRequest>, UpsertPriceRequestValidator>();
builder.Services.AddScoped<IValidator<ListPriceGroupsRequest>, ListPriceGroupsRequestValidator>();
builder.Services.AddScoped<IValidator<ListPricesRequest>, ListPricesRequestValidator>();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // The W3C trace id doubles as the correlation id: it is the value to paste into
    // Jaeger when someone reports a failed request.
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString();
    context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
});
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new CategoryIdJsonConverter());
    options.SerializerOptions.Converters.Add(new ProductIdJsonConverter());
    options.SerializerOptions.Converters.Add(new PriceGroupIdJsonConverter());
});

builder.Services.AddOpenApi(options => options.AddSchemaTransformer<TypedIdSchemaTransformer>());

builder.Services.AddHealthChecks()
    // The custom query matters: the default check only opens a connection, so a service
    // whose migrations never ran still reports itself ready and then fails every request
    // with "relation does not exist". Touching a table proves the schema is there too.
    .AddDbContextCheck<CatalogDbContext>(customTestQuery: async (dbContext, cancellationToken) =>
    {
        // The check passes when the query *runs*, not when it finds rows: an empty
        // catalog is a perfectly healthy catalog. Returning AnyAsync directly would
        // report a fresh database as unhealthy.
        _ = await dbContext.Categories.Take(1).CountAsync(cancellationToken);
        return true;
    });

var app = builder.Build();

// Request logging sits outside the exception handler on purpose. Inside it, every
// handled exception would be logged twice: once by Serilog as an in-flight 500, and
// again by the handler with the status the caller actually got.
app.UseSerilogRequestLogging();

// Anything thrown further down must still leave as RFC 7807.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    if (builder.Configuration.GetValue("Catalog:ApplyMigrationsAtStartup", defaultValue: true))
    {
        await MigrationRunner.ApplyAsync(app);
    }
}

// Liveness: answers as long as the process is up, and deliberately touches nothing.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).ExcludeFromDescription();

// Readiness: fails while the database is unreachable, so orchestrators hold traffic back.
app.MapHealthChecks("/health/ready");

app.MapCategoryEndpoints();
app.MapProductEndpoints();
app.MapPriceGroupEndpoints();

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot this host via WebApplicationFactory.</summary>
public partial class Program;
