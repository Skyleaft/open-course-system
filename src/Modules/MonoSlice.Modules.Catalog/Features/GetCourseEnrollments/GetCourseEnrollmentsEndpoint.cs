using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.GetCourseEnrollments;

public static class GetCourseEnrollmentsEndpoint
{
    public static void MapGetCourseEnrollmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{courseId:guid}/enrollments", async (
            Guid courseId,
            int? pageIndex,
            int? pageSize,
            string? search,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCourseEnrollmentsQuery(
                courseId,
                pageIndex ?? 1,
                pageSize ?? 20,
                search);

            var result = await mediator.Send(query, cancellationToken);
            return Results.Json(result, statusCode: result.StatusCode);
        })
        .WithName("GetCourseEnrollments")
        .WithSummary("Get list of enrolled students and their progress for a course (Instructor/Admin only)")
        .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
