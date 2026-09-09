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
/// responses as well as to the framework-generated ones. Per ADR-0018 the response
/// also carries a machine-readable <c>code</c> and the <c>params</c> a client needs to
/// render its own message; <c>detail</c> stays English for logs and bug reports.
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
        var (statusCode, title, translated) = Describe(exception);

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
            LogRejected(logger, statusCode, method, path, translated.Code, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = translated.Detail,
        };

        problemDetails.Extensions["code"] = translated.Code;

        if (translated.Parameters.Count > 0)
        {
            problemDetails.Extensions["params"] = translated.Parameters;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private static (int StatusCode, string Title, TranslatedError Error) Describe(Exception exception) =>
        exception switch
        {
            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Resource not found", TranslatedError.From(notFound, notFound.Message)),

            ConflictException conflict =>
                (StatusCodes.Status409Conflict, "Conflict", TranslatedError.From(conflict, conflict.Message)),

            // A unique index fired: two requests raced past a handler pre-check, or a
            // caller hit a constraint no handler checks at all.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } unique } =>
                (StatusCodes.Status409Conflict, "Conflict", DescribeUniqueViolation(unique)),

            // A restricted foreign key refused the write: something still points at the
            // row being deleted, or the row being referenced is gone.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } foreignKey } =>
                (StatusCodes.Status409Conflict, "Conflict", DescribeForeignKeyViolation(foreignKey)),

            DomainException domain =>
                (StatusCodes.Status400BadRequest, "Invalid request", TranslatedError.From(domain, domain.Message)),

            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred",
                  new TranslatedError(
                      CatalogErrorCodes.Unexpected,
                      "Please retry; if this keeps happening, quote the traceId when reporting it.")),
        };

    /// <summary>
    /// The index name is all the database gives us, so the value that clashed is not
    /// available here: this path only runs when two writers raced and the loser never
    /// saw the winning row. The code still tells the client which rule was broken.
    /// </summary>
    private static TranslatedError DescribeUniqueViolation(PostgresException postgres) =>
        postgres.ConstraintName switch
        {
            "ix_categories_slug" => new TranslatedError(
                CatalogErrorCodes.CategorySlugConflict,
                "A category with this slug already exists."),
            "ix_products_sku" => new TranslatedError(
                CatalogErrorCodes.ProductSkuConflict,
                "A product with this SKU already exists."),
            _ => TranslatedError.From(CatalogErrors.UnexpectedConflict()),
        };

    private static TranslatedError DescribeForeignKeyViolation(PostgresException postgres) =>
        postgres.ConstraintName switch
        {
            "fk_products_categories_category_id" => TranslatedError.From(CatalogErrors.CategoryHasProducts()),
            _ => new TranslatedError(
                CatalogErrorCodes.Conflict,
                "The request references data that does not exist, or is referenced by data that does."),
        };

    /// <summary>What the client is told: a stable code, its parameters, and an English explanation.</summary>
    private sealed record TranslatedError(
        string Code,
        string Detail,
        IReadOnlyDictionary<string, object?> Parameters)
    {
        private static readonly IReadOnlyDictionary<string, object?> NoParameters =
            new Dictionary<string, object?>();

        public TranslatedError(string code, string detail) : this(code, detail, NoParameters)
        {
        }

        public static TranslatedError From(IHasErrorCode error, string? detail = null) =>
            new(error.Code, detail ?? ((Exception)error).Message, error.Parameters);
    }

    // Source-generated logging: the message template is compiled once instead of being
    // parsed and boxed on every call. It is also what CA1848 asks for.
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled exception on {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Request rejected with {StatusCode} on {Method} {Path} as {Code}: {Reason}")]
    private static partial void LogRejected(
        ILogger logger, int statusCode, string method, string path, string code, string reason);
}
