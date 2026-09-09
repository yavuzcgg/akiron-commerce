using Akiron.Catalog.Api.Common;
using Akiron.Catalog.Application.Common;
using Akiron.Catalog.Application.Pricing;
using Akiron.Catalog.Application.Pricing.CreatePriceGroup;
using Akiron.Catalog.Application.Pricing.DeletePrice;
using Akiron.Catalog.Application.Pricing.DeletePriceGroup;
using Akiron.Catalog.Application.Pricing.GetPriceGroup;
using Akiron.Catalog.Application.Pricing.ListPriceGroups;
using Akiron.Catalog.Application.Pricing.ListPrices;
using Akiron.Catalog.Application.Pricing.UpdatePriceGroup;
using Akiron.Catalog.Application.Pricing.UpsertPrice;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Api.Pricing;

public static class PriceGroupEndpoints
{
    private const string GetPriceGroupRouteName = "GetPriceGroupByCode";

    public static void MapPriceGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/price-groups").WithTags("Price groups");

        group.MapGet("/", async (
                [AsParameters] ListPriceGroupsRequest request,
                ListPriceGroupsHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request, cancellationToken)))
            .Validate<ListPriceGroupsRequest>()
            .Produces<PagedResponse<PriceGroupResponse>>()
            .WithSummary("Lists price groups.");

        group.MapPost("/", async (
                CreatePriceGroupRequest request,
                CreatePriceGroupHandler handler,
                CancellationToken cancellationToken) =>
            {
                var priceGroup = await handler.HandleAsync(request, cancellationToken);
                return Results.CreatedAtRoute(GetPriceGroupRouteName, new { code = priceGroup.Code }, priceGroup);
            })
            .Validate<CreatePriceGroupRequest>()
            .Produces<PriceGroupResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Creates a price group. The currency is fixed at creation.");

        group.MapGet("/{code}", async (
                string code,
                GetPriceGroupHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(code, cancellationToken)))
            .WithName(GetPriceGroupRouteName)
            .Produces<PriceGroupResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Returns a single price group.");

        group.MapPut("/{code}", async (
                string code,
                UpdatePriceGroupRequest request,
                UpdatePriceGroupHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(code, request, cancellationToken)))
            .Validate<UpdatePriceGroupRequest>()
            .Produces<PriceGroupResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Updates the name and discount. The code and currency cannot change.");

        group.MapDelete("/{code}", async (
                string code,
                DeletePriceGroupHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(code, cancellationToken);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Deletes a price group. Fails while it still holds prices.");

        group.MapGet("/{code}/prices", async (
                string code,
                [AsParameters] ListPricesRequest request,
                ListPricesHandler handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(code, request, cancellationToken)))
            .Validate<ListPricesRequest>()
            .Produces<PagedResponse<PriceListEntryResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Lists the agreed prices in a group.");

        // PUT because the caller names the resource and the result is the same however
        // many times it is sent — 201 the first time, 200 after that.
        group.MapPut("/{code}/prices/{productId}", async (
                string code,
                ProductId productId,
                UpsertPriceRequest request,
                UpsertPriceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(code, productId, request, cancellationToken);

                return result.Created
                    ? Results.Created($"/api/v1/price-groups/{code}/prices/{productId.Value}", result.Price)
                    : Results.Ok(result.Price);
            })
            .Validate<UpsertPriceRequest>()
            .Produces<PriceListEntryResponse>(StatusCodes.Status201Created)
            .Produces<PriceListEntryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Sets the agreed price of a product in this group. The currency comes from the group.");

        group.MapDelete("/{code}/prices/{productId}", async (
                string code,
                ProductId productId,
                DeletePriceHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(code, productId, cancellationToken);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Removes an agreed price, falling the product back to the group discount.");
    }
}
