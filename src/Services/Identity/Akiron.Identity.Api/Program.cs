using System.Diagnostics;
using Akiron.Identity.Api.Common;
using Akiron.Identity.Api.Keys;
using Akiron.Identity.Api.Observability;
using Akiron.Identity.Api.Users;
using Akiron.Identity.Application.Users.GetCurrentUser;
using Akiron.Identity.Application.Users.Login;
using Akiron.Identity.Application.Users.Register;
using Akiron.Identity.Infrastructure;
using Akiron.Identity.Infrastructure.Persistence;
using Akiron.Identity.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Fail fast on missing configuration: at boot, not on the first request.
var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? throw new InvalidOperationException("ConnectionStrings:IdentityDb is not configured.");

// The private key is loaded once, here. A relative path is resolved against the content
// root so F5 and `dotnet run` find the same file. Only Development may generate one.
var signingKeyPath = Path.Combine(
    builder.Environment.ContentRootPath,
    builder.Configuration["Identity:SigningKeyPath"] ?? "dev-keys/identity-signing-key.pem");
var keyStore = SigningKeyStore.Load(
    signingKeyPath,
    createIfMissing: builder.Configuration.GetValue("Identity:GenerateSigningKeyIfMissing", builder.Environment.IsDevelopment()));
var tokenSettings = TokenSettings.Default;

// Serilog, telemetry, ProblemDetails and health checks below are copied from Catalog
// on purpose. Two services now carry the same code, which is what allows slice 2.2 to
// extract it into Akiron.ServiceDefaults.
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddIdentityTelemetry(builder.Configuration);
builder.Services.AddIdentityInfrastructure(connectionString, keyStore, tokenSettings);
builder.Services.AddBearerAuthentication(keyStore, tokenSettings);

// Registered one by one, as in Catalog: the wired-up set stays greppable.
builder.Services.AddScoped<RegisterHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<GetCurrentUserHandler>();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString();
    context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
});
builder.Services.AddExceptionHandler<IdentityExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new UserIdJsonConverter()));

builder.Services.AddOpenApi(options => options.AddSchemaTransformer<TypedIdSchemaTransformer>());

builder.Services.AddHealthChecks()
    // Touching a table proves the migrations ran, not just that the server answers.
    .AddDbContextCheck<IdentityServiceDbContext>(customTestQuery: async (dbContext, cancellationToken) =>
    {
        _ = await dbContext.Users.Take(1).CountAsync(cancellationToken);
        return true;
    });

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    if (builder.Configuration.GetValue("Identity:ApplyMigrationsAtStartup", defaultValue: true))
    {
        await MigrationRunner.ApplyAsync(app);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).ExcludeFromDescription();
app.MapHealthChecks("/health/ready");

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapJwksEndpoint();

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot this host via WebApplicationFactory.</summary>
public partial class Program;
