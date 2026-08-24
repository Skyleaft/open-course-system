using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.GetEnrolledCourses;

public static class GetEnrolledCoursesEndpoint
{
    public static void MapGetEnrolledCoursesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/enrolled", async (
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetEnrolledCoursesQuery();
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetEnrolledCourses")
            .WithSummary("List all courses the authenticated student is currently enrolled in with progression stats")
            .RequireAuthorization();
    }
}
