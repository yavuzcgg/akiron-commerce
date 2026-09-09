using Akiron.Catalog.Domain.Common;
using FluentValidation;

namespace Akiron.Catalog.Application.Common;

/// <summary>One page of results, plus what a client needs to ask for the next one.</summary>
/// <remarks>
/// <c>totalCount</c> costs a second COUNT query. It is worth it because the admin tables
/// show page numbers, which cannot be drawn from a "has more" flag. The page count is
/// left to the client to divide out rather than sent twice.
/// </remarks>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// Paging and sorting parameters shared by every list endpoint.
/// </summary>
/// <remarks>
/// Offset paging (<c>Skip</c>/<c>Take</c>): it is what page-numbered tables need. Deep
/// pages get slower as the offset grows, which is documented in API_CONVENTIONS.md and
/// stops mattering for the storefront once search moves to Elasticsearch in Faz 3.
/// </remarks>
public record PagingRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Sort { get; init; }

    public string? Direction { get; init; }

    public int ResolvedPage => Page ?? DefaultPage;

    public int ResolvedPageSize => PageSize ?? DefaultPageSize;

    public bool IsDescending =>
        string.Equals(Direction, "desc", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Shared paging rules. Oversized pages are rejected rather than clamped: silently
/// returning something other than what was asked for is how a client ends up believing
/// it has read everything when it has not.
/// </summary>
public class PagingRequestValidator<T> : AbstractValidator<T>
    where T : PagingRequest
{
    protected PagingRequestValidator(IReadOnlyCollection<string> sortableFields)
    {
        // Written against the nullable property rather than its .Value, so the error is
        // reported under "page" instead of "page.Value" — the field name is part of the
        // contract a client reads.
        RuleFor(request => request.Page)
            .Must(page => page is null or >= 1)
            .WithErrorCode(CatalogErrorCodes.PageOutOfRange)
            .WithMessage("'{PropertyName}' must be 1 or greater.");

        RuleFor(request => request.PageSize)
            .Must(pageSize => pageSize is null || (pageSize >= 1 && pageSize <= PagingRequest.MaxPageSize))
            .WithErrorCode(CatalogErrorCodes.PageSizeTooLarge)
            .WithMessage($"'{{PropertyName}}' must be between 1 and {PagingRequest.MaxPageSize}.");

        RuleFor(request => request.Sort)
            .Must(sort => sort is null || sortableFields.Contains(sort, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode(CatalogErrorCodes.SortNotSupported)
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", sortableFields)}.");

        RuleFor(request => request.Direction)
            .Must(direction => direction is null
                || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithErrorCode(CatalogErrorCodes.SortNotSupported)
            .WithMessage("'{PropertyName}' must be asc or desc.");
    }
}
