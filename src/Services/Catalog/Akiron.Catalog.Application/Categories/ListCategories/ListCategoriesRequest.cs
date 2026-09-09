using Akiron.Catalog.Application.Common;

namespace Akiron.Catalog.Application.Categories.ListCategories;

public sealed record ListCategoriesRequest : PagingRequest
{
    public static readonly string[] SortableFields = ["name", "createdAt"];
}

public sealed class ListCategoriesRequestValidator()
    : PagingRequestValidator<ListCategoriesRequest>(ListCategoriesRequest.SortableFields);
