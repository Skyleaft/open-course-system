using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.UpdateSection;

public static class UpdateSectionEndpoint
{
    public static void MapUpdateSectionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/sections/{sectionId:guid}", async (
                Guid sectionId,
                UpdateSectionRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new UpdateSectionCommand
                {
                    SectionId = sectionId,
                    Title = request.Title,
                    OrderIndex = request.OrderIndex
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("UpdateCourseSection")
            .WithSummary("Update a curriculum section (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record UpdateSectionRequest(string Title, int? OrderIndex);
