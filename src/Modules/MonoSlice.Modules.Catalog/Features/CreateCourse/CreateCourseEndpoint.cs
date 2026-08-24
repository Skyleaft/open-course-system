using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.CreateCourse;

public static class CreateCourseEndpoint
{
    public static void MapCreateCourseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", async (
                CreateCourseCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("CreateCourse")
            .WithSummary("Create a new course (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
