using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Application.Products;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Application.Products.CreateProduct;
using Akiron.Catalog.Application.Products.DeleteProduct;
using Akiron.Catalog.Application.Products.GetProduct;
using Akiron.Catalog.Application.Products.ListProducts;
using Akiron.Catalog.Application.Products.UpdateProduct;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Api.Products;

public static class ProductEndpoints
{
    private const string GetProductRouteName = "GetProductById";

    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("/", async (
                [AsParameters] ListProductsRequest request,
                ListProductsHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request, cancellationToken)))
            .Validate<ListProductsRequest>()
            .Produces<PagedResponse<ProductResponse>>()
            .WithSummary("Lists products. Filters are exact matches; text search arrives with Elasticsearch in Faz 3.");

        group.MapPost("/", async (
                CreateProductRequest request,
                CreateProductHandler handler,
                CancellationToken cancellationToken) =>
            {
                var product = await handler.HandleAsync(request, cancellationToken);
                return Results.CreatedAtRoute(GetProductRouteName, new { productId = product.Id.Value }, product);
            })
            .Validate<CreateProductRequest>()
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Creates a product.");

        group.MapGet("/{productId}", async (
                ProductId productId,
                GetProductHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(productId, cancellationToken)))
            .WithName(GetProductRouteName)
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Returns a single product.");

        group.MapPut("/{productId}", async (
                ProductId productId,
                UpdateProductRequest request,
                UpdateProductHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(productId, request, cancellationToken)))
            .Validate<UpdateProductRequest>()
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Updates a product. The SKU cannot be changed.");

        group.MapDelete("/{productId}", async (
                ProductId productId,
                DeleteProductHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(productId, cancellationToken);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Deletes a product.");
    }
}
