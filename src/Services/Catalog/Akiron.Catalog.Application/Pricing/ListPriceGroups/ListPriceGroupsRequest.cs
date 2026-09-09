using Akiron.Catalog.Application.Common;

namespace Akiron.Catalog.Application.Pricing.ListPriceGroups;

public sealed record ListPriceGroupsRequest : PagingRequest
{
    public static readonly string[] SortableFields = ["code", "name", "createdAt"];
}

public sealed class ListPriceGroupsRequestValidator()
    : PagingRequestValidator<ListPriceGroupsRequest>(ListPriceGroupsRequest.SortableFields);
