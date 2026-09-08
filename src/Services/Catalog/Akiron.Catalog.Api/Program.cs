using System.Diagnostics;
using Akiron.Catalog.Api.Categories;
using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Application.Categories.CreateCategory;
using Akiron.Catalog.Application.Categories.GetCategory;
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
builder.Services.AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryRequestValidator>();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // The W3C trace id doubles as the correlation id: it is the value to paste into
    // Jaeger when someone reports a failed request.
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString();
    context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
});
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new CategoryIdJsonConverter()));

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

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot this host via WebApplicationFactory.</summary>
public partial class Program;
