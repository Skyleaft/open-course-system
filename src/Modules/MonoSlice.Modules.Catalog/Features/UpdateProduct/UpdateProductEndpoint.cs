using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Catalog.Features.UpdateProduct;

public static class UpdateProductEndpoint
{
    public static void MapUpdateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest(ApiResponse.Fail("Route ID and payload ID mismatch."));
            }

            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("UpdateProduct")
        .WithSummary("Updates an existing product in the catalog (Admin/Manager only)")
        .Produces<ApiResponse<ProductDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse>(StatusCodes.Status404NotFound)
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));
    }
}
