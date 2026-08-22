using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Orders.Features.GetOrder;

public static class GetOrderEndpoint
{
    public static IEndpointRouteBuilder MapGetOrderEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetOrderQuery(id), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrder")
        .WithSummary("Get order by ID")
        .Produces<ApiResponse<OrderDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        return app;
    }
}
