using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.AddSection;

public static class AddSectionEndpoint
{
    public static void MapAddSectionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/sections", async (
                Guid id,
                AddSectionRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new AddSectionCommand
                {
                    CourseId = id,
                    Title = request.Title
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("AddCourseSection")
            .WithSummary("Add a curriculum section to a course (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record AddSectionRequest(string Title);
