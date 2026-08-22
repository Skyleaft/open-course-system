using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Catalog.Features.ListProducts;

public static class ListProductsEndpoint
{
    public static void MapListProductsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListProductsQuery(
                pageNumber ?? 1,
                pageSize ?? 10,
                search,
                isActive);

            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ListProducts")
        .WithSummary("Lists products with pagination and filtering")
        .Produces<ApiResponse<PaginatedList<ProductDto>>>(StatusCodes.Status200OK)
        .AllowAnonymous();
    }
}
