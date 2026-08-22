using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Orders.Features.ListOrders;

public static class ListOrdersEndpoint
{
    public static IEndpointRouteBuilder MapListOrdersEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (
            int? pageNumber,
            int? pageSize,
            Guid? customerId,
            OrderStatus? status,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new ListOrdersQuery(
                pageNumber ?? 1,
                pageSize ?? 10,
                customerId,
                status);

            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("ListOrders")
        .WithSummary("List orders with pagination and filtering")
        .Produces<ApiResponse<PaginatedList<OrderDto>>>(StatusCodes.Status200OK);

        return app;
    }
}
