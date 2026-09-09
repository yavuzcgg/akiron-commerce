using Akiron.Catalog.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Runs the FluentValidation validator for a request before the handler sees it.
/// </summary>
public static class ValidationEndpointFilter
{
    /// <summary>
    /// Usage: <c>group.MapPost("/", Handler).Validate&lt;CreateCategoryRequest&gt;();</c>
    /// </summary>
    public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder
            .AddEndpointFilter(async (invocationContext, next) =>
            {
                var request = invocationContext.Arguments.OfType<TRequest>().FirstOrDefault();

                if (request is null)
                {
                    return await next(invocationContext);
                }

                var validator = invocationContext.HttpContext.RequestServices.GetRequiredService<IValidator<TRequest>>();
                var result = await validator.ValidateAsync(request, invocationContext.HttpContext.RequestAborted);

                if (result.IsValid)
                {
                    return await next(invocationContext);
                }

                // Each failure carries its code alongside the English message (ADR-0018),
                // which is why this builds the response instead of calling
                // Results.ValidationProblem: that helper only knows how to send strings.
                var errors = result.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => ToCamelCase(group.Key),
                        group => group
                            .Select(failure => new FieldError(
                                string.IsNullOrEmpty(failure.ErrorCode) ? CatalogErrorCodes.ValidationFailed : failure.ErrorCode,
                                failure.ErrorMessage))
                            .Distinct()
                            .ToArray());

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                };

                problemDetails.Extensions["code"] = CatalogErrorCodes.ValidationFailed;
                problemDetails.Extensions["errors"] = errors;

                var problemDetailsService = invocationContext.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();

                invocationContext.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = invocationContext.HttpContext,
                    ProblemDetails = problemDetails,
                });

                return Results.Empty;
            })
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? propertyName
            : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];

    /// <summary>One rejected rule: the code a client branches on, and English for the log.</summary>
    private sealed record FieldError(string Code, string Message);
}
