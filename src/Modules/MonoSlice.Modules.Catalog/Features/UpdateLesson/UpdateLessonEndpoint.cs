using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Catalog.Domain;

namespace MonoSlice.Modules.Catalog.Features.UpdateLesson;

public static class UpdateLessonEndpoint
{
    public static void MapUpdateLessonEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/lessons/{lessonId:guid}", async (
                Guid lessonId,
                UpdateLessonRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new UpdateLessonCommand
                {
                    LessonId = lessonId,
                    Title = request.Title,
                    Type = request.Type,
                    ContentUrl = request.ContentUrl,
                    TextContent = request.TextContent,
                    DurationMinutes = request.DurationMinutes,
                    OrderIndex = request.OrderIndex
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("UpdateSectionLesson")
            .WithSummary("Update a lesson (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record UpdateLessonRequest(
    string Title,
    LessonType Type,
    string? ContentUrl,
    string? TextContent,
    int DurationMinutes,
    int? OrderIndex);
