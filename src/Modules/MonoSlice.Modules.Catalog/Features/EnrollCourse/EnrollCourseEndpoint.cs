using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.EnrollCourse;

public static class EnrollCourseEndpoint
{
    public static void MapEnrollCourseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/enroll", async (
                Guid id,
                EnrollCourseRequest? request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new EnrollCourseCommand
                {
                    CourseId = id,
                    EnrollmentKey = request?.EnrollmentKey
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("EnrollInCourse")
            .WithSummary("Enroll current user into a course")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}

public sealed record EnrollCourseRequest(string? EnrollmentKey = null);
