using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MonoSlice.Modules.Catalog.Domain;

namespace MonoSlice.Modules.Catalog.Features.AddLesson;

public static class AddLessonEndpoint
{
    public static void MapAddLessonEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/sections/{sectionId:guid}/lessons", async (
                Guid sectionId,
                AddLessonRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new AddLessonCommand
                {
                    SectionId = sectionId,
                    Title = request.Title,
                    Type = request.Type,
                    ContentUrl = request.ContentUrl,
                    DurationMinutes = request.DurationMinutes
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AddSectionLesson")
            .WithSummary("Add a lesson to a course section (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record AddLessonRequest(
    string Title,
    LessonType Type,
    string ContentUrl,
    int DurationMinutes);
