using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetCandidateSnapshots;

public static class GetCandidateSnapshotsEndpoint
{
    public static void MapGetCandidateSnapshotsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/submissions/{submissionId:guid}/snapshots", async (
                Guid submissionId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetCandidateSnapshotsQuery(submissionId);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetCandidateSnapshots")
            .WithSummary("Retrieve candidate snapshot timeline gallery with presigned view URLs")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin", "Proctor"));
    }
}

