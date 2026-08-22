using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.PublishCourse;

public static class PublishCourseEndpoint
{
    public static void MapPublishCourseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/publish", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new PublishCourseCommand(id, Publish: true);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("PublishCourse")
            .WithSummary("Publish a course (Instructor/Admin only)")
            .RequireAuthorization();
    }
}
