using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Catalog.Features.CreateProduct;

public static class CreateProductEndpoint
{
    public static void MapCreateProductEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", async (
            CreateProductCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/catalog/products/{result.Data?.Id}", result);
        })
        .WithName("CreateProduct")
        .WithSummary("Creates a new product in the catalog (Admin/Manager only)")
        .WithDescription("Creates a new catalog product with GuidV7 ID and publishes an async integration event.")
        .Produces<ApiResponse<ProductDto>>(StatusCodes.Status201Created)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse>(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));
    }
}
