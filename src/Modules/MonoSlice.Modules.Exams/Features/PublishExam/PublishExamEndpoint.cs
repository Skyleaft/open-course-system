using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.PublishExam;

public static class PublishExamEndpoint
{
    public static void MapPublishExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/{id:guid}/publish", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new PublishExamCommand(id, Publish: true);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("PublishExam")
            .WithSummary("Publish an exam (Instructor/Admin only)")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}
