using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Orders.Features.CreateCheckout;

public static class CreateCheckoutEndpoint
{
    public static void MapCreateCheckoutEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/checkout", async (
                CreateCheckoutCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("CreateCheckout")
            .WithSummary("Initiate a course purchase checkout order")
            .RequireAuthorization();
    }
}
