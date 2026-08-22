using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Orders.Features.GetOrder;

public static class GetOrderEndpoint
{
    public static void MapGetOrderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/orders/{id:guid}", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetOrderQuery(id);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetPaymentOrderById")
            .WithSummary("Get payment order details and status by ID")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
