using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetProctorLiveSummary;

public static class GetProctorLiveSummaryEndpoint
{
    public static void MapGetProctorLiveSummaryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/proctor/live-summary", async (
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetProctorLiveSummaryQuery();
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetProctorLiveSummary")
            .WithSummary("Get active exams, examinee counts, and flagged high-risk candidates")
            .RequireAuthorization(policy => policy.RequireRole("Proctor", "Instructor", "Admin"));
    }
}
