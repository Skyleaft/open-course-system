using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Orders.Features.ProcessOrderAsync;

public static class ProcessOrderAsyncEndpoint
{
    public static IEndpointRouteBuilder MapProcessOrderAsyncEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{id:guid}/process-async", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ProcessOrderAsyncCommand(id), ct);
            return Results.Accepted($"/api/orders/{id}", result);
        })
        .WithName("ProcessOrderAsync")
        .WithSummary("Trigger asynchronous background order processing")
        .WithDescription("Enqueues the specified order into the background worker channel queue for asynchronous simulation of payment, fulfillment, and integration event emission.")
        .Produces<ApiResponse<string>>(StatusCodes.Status202Accepted)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        return app;
    }
}
