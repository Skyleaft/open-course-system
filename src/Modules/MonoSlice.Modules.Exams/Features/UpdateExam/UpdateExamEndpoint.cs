using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.UpdateExam;

public static class UpdateExamEndpoint
{
    public static void MapUpdateExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/{id:guid}", async (
                Guid id,
                UpdateExamCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var enrichedCommand = command with { Id = id };
                var response = await mediator.Send(enrichedCommand, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("UpdateExam")
            .WithSummary("Update exam parameters (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
