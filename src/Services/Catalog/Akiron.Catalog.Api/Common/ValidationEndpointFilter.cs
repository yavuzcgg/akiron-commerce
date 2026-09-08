using FluentValidation;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Runs the FluentValidation validator for a request body before the handler sees it.
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

                var errors = result.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => char.ToLowerInvariant(group.Key[0]) + group.Key[1..],
                        group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

                // Results.ValidationProblem writes through IProblemDetailsService, so the
                // traceId extension configured in Program.cs lands on this response too.
                return Results.ValidationProblem(errors);
            })
            .ProducesValidationProblem();
    }
}
