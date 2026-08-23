using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.DeleteLesson;

public static class DeleteLessonEndpoint
{
    public static void MapDeleteLessonEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/lessons/{lessonId:guid}", async (
                Guid lessonId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new DeleteLessonCommand(lessonId);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("DeleteSectionLesson")
            .WithSummary("Delete a lesson (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
