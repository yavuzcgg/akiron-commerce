using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Akiron.Identity.Api.Common;

/// <summary>
/// Turns every escaping exception into an RFC 7807 response with an error code (ADR-0018).
/// </summary>
/// <remarks>A copy of Catalog's handler with Identity's mappings; one of 2.2's extraction candidates.</remarks>
public sealed partial class IdentityExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<IdentityExceptionHandler> logger) : IExceptionHandler
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters = new Dictionary<string, object?>();

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, error) = Describe(exception);

        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, method, path);
        }
        else
        {
            LogRejected(logger, statusCode, method, path, error.Code);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails { Status = statusCode, Title = title, Detail = error.Detail };
        problemDetails.Extensions["code"] = error.Code;

        if (error.Parameters.Count > 0)
        {
            problemDetails.Extensions["params"] = error.Parameters;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private static (int StatusCode, string Title, Error Error) Describe(Exception exception) =>
        exception switch
        {
            InvalidCredentialsException invalid =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", Error.From(invalid)),

            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Resource not found", Error.From(notFound)),

            ConflictException conflict =>
                (StatusCodes.Status409Conflict, "Conflict", Error.From(conflict)),

            // Two registrations raced past the handler's pre-check; the index decided.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } unique } =>
                (StatusCodes.Status409Conflict, "Conflict", unique.ConstraintName == "ix_users_email"
                    ? Error.From(IdentityErrors.EmailTaken())
                    : Error.From(IdentityErrors.UnexpectedConflict())),

            DomainException domain =>
                (StatusCodes.Status400BadRequest, "Invalid request", Error.From(domain)),

            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred",
                  new Error(
                      IdentityErrorCodes.Unexpected,
                      "Please retry; if this keeps happening, quote the traceId when reporting it.",
                      NoParameters)),
        };

    private sealed record Error(string Code, string Detail, IReadOnlyDictionary<string, object?> Parameters)
    {
        public static Error From(IHasErrorCode error) =>
            new(error.Code, ((Exception)error).Message, error.Parameters);
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled exception on {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);

    // No exception message in this one: for a failed login it would be the same for
    // everyone anyway, and nothing here should ever log a submitted credential.
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Request rejected with {StatusCode} on {Method} {Path} as {Code}")]
    private static partial void LogRejected(ILogger logger, int statusCode, string method, string path, string code);
}
