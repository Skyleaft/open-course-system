using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.Admin.GetSystemHealth;

public static class GetSystemHealthEndpoint
{
    public static void MapGetSystemHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/admin/system-health", async (
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetSystemHealthQuery();
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetAdminSystemHealth")
            .WithSummary("Get system health, dead-letter queue metrics, and worker status")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
