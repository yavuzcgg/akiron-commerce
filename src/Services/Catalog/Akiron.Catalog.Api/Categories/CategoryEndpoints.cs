using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Application.Categories;
using Akiron.Catalog.Application.Categories.CreateCategory;
using Akiron.Catalog.Application.Categories.DeleteCategory;
using Akiron.Catalog.Application.Categories.GetCategory;
using Akiron.Catalog.Application.Categories.UpdateCategory;
using Akiron.Catalog.Domain.Categories;

namespace Akiron.Catalog.Api.Categories;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories").WithTags("Categories");

        group.MapPost("/", async (
                CreateCategoryRequest request,
                CreateCategoryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var category = await handler.HandleAsync(request, cancellationToken);
                return Results.CreatedAtRoute(GetCategoryRouteName, new { categoryId = category.Id.Value }, category);
            })
            .Validate<CreateCategoryRequest>()
            .Produces<CategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Creates a category.");

        // CategoryId binds straight from the route because it implements IParsable.
        group.MapGet("/{categoryId}", async (
                CategoryId categoryId,
                GetCategoryHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(categoryId, cancellationToken)))
            .WithName(GetCategoryRouteName)
            .Produces<CategoryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Returns a single category.");

        group.MapPut("/{categoryId}", async (
                CategoryId categoryId,
                UpdateCategoryRequest request,
                UpdateCategoryHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(categoryId, request, cancellationToken)))
            .Validate<UpdateCategoryRequest>()
            .Produces<CategoryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Renames a category. The slug cannot be changed: it is part of public URLs.");

        group.MapDelete("/{categoryId}", async (
                CategoryId categoryId,
                DeleteCategoryHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(categoryId, cancellationToken);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Deletes a category. Fails while products still reference it.");
    }

    private const string GetCategoryRouteName = "GetCategoryById";
}
