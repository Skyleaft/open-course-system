using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.DeleteSection;

public static class DeleteSectionEndpoint
{
    public static void MapDeleteSectionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/sections/{sectionId:guid}", async (
                Guid sectionId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new DeleteSectionCommand(sectionId);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("DeleteCourseSection")
            .WithSummary("Delete a curriculum section and its lessons (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
