using Akiron.Catalog.Application.Common;

namespace Akiron.Catalog.Application.Pricing.ListPrices;

public sealed record ListPricesRequest : PagingRequest
{
    public static readonly string[] SortableFields = ["amount", "createdAt"];
}

public sealed class ListPricesRequestValidator()
    : PagingRequestValidator<ListPricesRequest>(ListPricesRequest.SortableFields);
