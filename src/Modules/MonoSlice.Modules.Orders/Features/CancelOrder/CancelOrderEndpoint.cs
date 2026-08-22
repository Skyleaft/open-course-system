using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Orders.Features.CancelOrder;

public static class CancelOrderEndpoint
{
    public static IEndpointRouteBuilder MapCancelOrderEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{id:guid}/cancel", async (Guid id, CancelOrderRequest? request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new CancelOrderCommand(id, request?.Reason);
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CancelOrder")
        .WithSummary("Cancel an order")
        .Produces<ApiResponse<string>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record CancelOrderRequest(string? Reason);
