using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.GetCourseProgress;

public static class GetCourseProgressEndpoint
{
    public static void MapGetCourseProgressEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{courseId:guid}/progress", async (
                Guid courseId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetCourseProgressQuery(courseId);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetCourseProgress")
            .WithSummary("Retrieve student progression and completion data for a specific course")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
