using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.UpdateCourse;

public static class UpdateCourseEndpoint
{
    public static void MapUpdateCourseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{id:guid}", async (
                Guid id,
                UpdateCourseCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enrichedCommand = command with { Id = id };
                var response = await mediator.Send(enrichedCommand, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("UpdateCourse")
            .WithSummary("Update course details (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
