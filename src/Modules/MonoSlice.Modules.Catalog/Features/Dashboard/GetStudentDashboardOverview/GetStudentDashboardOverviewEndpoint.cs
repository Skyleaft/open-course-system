using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.Dashboard.GetStudentDashboardOverview;

public static class GetStudentDashboardOverviewEndpoint
{
    public static void MapGetStudentDashboardOverviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/student/overview", async (
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetStudentDashboardOverviewQuery();
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetStudentDashboardOverview")
            .WithSummary("Get student overview with active enrolled courses, upcoming deadlines, and competency points")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
