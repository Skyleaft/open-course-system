using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Modules.Orders.Features.GetOrderAnalytics;

public static class GetOrderAnalyticsEndpoint
{
    public static void MapGetOrderAnalyticsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/analytics", async (
            Guid? customerId,
            DateTime? fromDate,
            DateTime? toDate,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrderAnalyticsQuery(customerId, fromDate, toDate);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetOrderAnalytics")
        .WithSummary("Retrieves complex order analytics using Dapper (Admin/Manager)")
        .WithDescription("Executes high-performance multi-query aggregations across orders and order items via Dapper.")
        .Produces<ApiResponse<OrderAnalyticsDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));

        app.MapGet("/analytics/customer/{customerId:guid}", async (
            Guid customerId,
            DateTime? fromDate,
            DateTime? toDate,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrderAnalyticsQuery(customerId, fromDate, toDate);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCustomerOrderAnalytics")
        .WithSummary("Retrieves order analytics for a specific customer using Dapper")
        .WithDescription("Executes high-performance Dapper multi-table aggregations for an individual customer.")
        .Produces<ApiResponse<OrderAnalyticsDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}
