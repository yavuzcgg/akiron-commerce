using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Turns every escaping exception into an RFC 7807 response.
/// </summary>
/// <remarks>
/// Written against <see cref="IProblemDetailsService"/> rather than serialising by
/// hand, so the traceId extension configured in Program.cs applies to these
/// responses as well as to the framework-generated ones.
/// </remarks>
public sealed partial class CatalogExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<CatalogExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Describe(exception);

        // Read out of the request once: PathString converts to string implicitly, and
        // doing that inside the logging call trips CA1873.
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, method, path);
        }
        else
        {
            // Expected rejections are not warnings: a 404 is the API working.
            LogRejected(logger, statusCode, method, path, detail);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }

    private static (int StatusCode, string Title, string Detail) Describe(Exception exception) => exception switch
    {
        NotFoundException notFound =>
            (StatusCodes.Status404NotFound, "Resource not found", notFound.Message),

        ConflictException conflict =>
            (StatusCodes.Status409Conflict, "Conflict", conflict.Message),

        // A unique index fired: two requests raced past the handler's pre-check, or a
        // caller hit a constraint the handler does not check at all. Either way the
        // caller should see the same 409 the pre-check would have produced.
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres } =>
            (StatusCodes.Status409Conflict, "Conflict", DescribeUniqueViolation(postgres)),

        // A restricted foreign key refused the write: something still points at the row
        // being deleted, or the row being referenced is gone.
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } foreignKey } =>
            (StatusCodes.Status409Conflict, "Conflict", DescribeForeignKeyViolation(foreignKey)),

        DomainException domain =>
            (StatusCodes.Status400BadRequest, "Invalid request", domain.Message),

        _ => (StatusCodes.Status500InternalServerError,
              "An unexpected error occurred",
              "Please retry; if this keeps happening, quote the traceId when reporting it."),
    };

    private static string DescribeUniqueViolation(PostgresException postgres) => postgres.ConstraintName switch
    {
        "ix_categories_slug" => "A category with this slug already exists.",
        "ix_products_sku" => "A product with this SKU already exists.",
        _ => "The request conflicts with data that already exists.",
    };

    private static string DescribeForeignKeyViolation(PostgresException postgres) => postgres.ConstraintName switch
    {
        "fk_products_categories_category_id" =>
            "This category still has products. Move or delete them before deleting the category.",
        _ => "The request references data that does not exist, or is referenced by data that does.",
    };

    // Source-generated logging: the message template is compiled once instead of being
    // parsed and boxed on every call. It is also what CA1848 asks for.
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled exception on {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Request rejected with {StatusCode} on {Method} {Path}: {Reason}")]
    private static partial void LogRejected(ILogger logger, int statusCode, string method, string path, string reason);
}
