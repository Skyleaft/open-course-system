using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.PresignSnapshot;

public static class PresignSnapshotEndpoint
{
    public static void MapPresignSnapshotEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/submissions/{submissionId:guid}/snapshots/presign", async (
                Guid submissionId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new PresignSnapshotCommand { SubmissionId = submissionId };
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("PresignExamSnapshot")
            .WithSummary("Generate 2-minute presigned S3 upload URL for anti-cheat proctor snapshots")
            .RequireAuthorization();
    }
}
