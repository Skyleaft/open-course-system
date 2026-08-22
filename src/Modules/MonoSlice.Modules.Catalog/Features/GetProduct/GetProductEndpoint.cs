using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Catalog.Features.GetProduct;

public static class GetProductEndpoint
{
    public static void MapGetProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetProductQuery(id), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetProductById")
        .WithSummary("Retrieves a product by ID (cached)")
        .Produces<ApiResponse<ProductDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status404NotFound)
        .AllowAnonymous();
    }
}
