using Akiron.Catalog.Application.Common;

namespace Akiron.Catalog.Application.Products.ListProducts;

/// <summary>
/// Filters are exact matches only.
/// </summary>
/// <remarks>
/// There is deliberately no name search. PostgreSQL ILIKE gets Turkish wrong — measured:
/// 'KIS LASTIGI' ILIKE '%kis%' with the dotted and dotless i is false — so a search box
/// built on it would quietly miss products. Real search arrives in Faz 3 with
/// Elasticsearch and a Turkish analyzer (ADR-0008). SKU filtering is safe because SKUs
/// are ASCII and already upper-cased by the value object.
/// </remarks>
public sealed record ListProductsRequest : PagingRequest
{
    public static readonly string[] SortableFields = ["name", "createdAt", "basePrice"];

    public Guid? CategoryId { get; init; }

    public bool? IsActive { get; init; }

    public string? Sku { get; init; }
}

public sealed class ListProductsRequestValidator()
    : PagingRequestValidator<ListProductsRequest>(ListProductsRequest.SortableFields);
