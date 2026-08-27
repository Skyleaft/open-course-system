using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Analytics.GetSecurityViolationsSummary;

public static class GetSecurityViolationsSummaryEndpoint
{
    public static void MapGetSecurityViolationsSummaryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/admin/security-violations", async (
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetSecurityViolationsSummaryQuery();
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetAdminSecurityViolations")
            .WithSummary("Get global security violations, disqualification rates, and high-risk exams")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
