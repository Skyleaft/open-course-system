using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.SubmitExam;

public static class SubmitExamEndpoint
{
    public static void MapSubmitExamEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/submissions/{submissionId:guid}/finish", async (
                Guid submissionId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new SubmitExamCommand(submissionId);
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("FinishAndSubmitExam")
            .WithSummary("Finalize exam attempt, trigger automated scoring, and publish exam submitted event")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
