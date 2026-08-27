using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Orders.Features.GetRevenueAnalytics;

public static class GetRevenueAnalyticsEndpoint
{
    public static void MapGetRevenueAnalyticsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/admin/revenue-analytics", async (
                DateTime? fromUtc,
                DateTime? toUtc,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetRevenueAnalyticsQuery
                {
                    FromUtc = fromUtc,
                    ToUtc = toUtc
                };
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetAdminRevenueAnalytics")
            .WithSummary("Get revenue analytics, GMV trends, and top performing courses")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
