using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.ForceDisconnectCandidate;

public static class ForceDisconnectCandidateEndpoint
{
    public static void MapForceDisconnectCandidateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/submissions/{submissionId:guid}/force-disconnect", async (
                Guid submissionId,
                ForceDisconnectRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new ForceDisconnectCandidateCommand
                {
                    SubmissionId = submissionId,
                    Reason = request.Reason ?? "Disqualified by Proctor"
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("ForceDisconnectCandidate")
            .WithSummary("Proctor forcibly terminates and disqualifies exam candidate")
            .RequireAuthorization();
    }
}

public sealed record ForceDisconnectRequest(string? Reason);
