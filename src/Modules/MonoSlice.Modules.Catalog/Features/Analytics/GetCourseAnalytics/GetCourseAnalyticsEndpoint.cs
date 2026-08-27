using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.Analytics.GetCourseAnalytics;

public static class GetCourseAnalyticsEndpoint
{
    public static void MapGetCourseAnalyticsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dashboard/instructor/courses/{courseId:guid}/analytics", async (
                Guid courseId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetCourseAnalyticsQuery { CourseId = courseId };
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetInstructorCourseAnalytics")
            .WithSummary("Get detailed course analytics, funnel completion rates, and drop-off per section")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
