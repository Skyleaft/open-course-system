using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.WarnCandidate;

public static class WarnCandidateEndpoint
{
    public static void MapWarnCandidateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/submissions/{submissionId:guid}/warn", async (
                Guid submissionId,
                WarnCandidateRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new WarnCandidateCommand
                {
                    SubmissionId = submissionId,
                    Message = request.Message
                };

                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("WarnCandidate")
            .WithSummary("Proctor sends real-time custom warning modal to candidate")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record WarnCandidateRequest(string Message);
