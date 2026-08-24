using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.CompleteLesson;

public static class CompleteLessonEndpoint
{
    public static void MapCompleteLessonEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{courseId:guid}/lessons/{lessonId:guid}/complete", async (
                Guid courseId,
                Guid lessonId,
                CompleteLessonRequest? request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new CompleteLessonCommand(courseId, lessonId, request?.IsCompleted);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("CompleteCourseLesson")
            .WithSummary("Mark or toggle a lesson as completed for the authenticated student")
            .RequireAuthorization();
    }
}

public sealed record CompleteLessonRequest(bool? IsCompleted);
