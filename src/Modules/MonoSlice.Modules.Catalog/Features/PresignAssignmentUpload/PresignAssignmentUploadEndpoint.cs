using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.PresignAssignmentUpload;

public static class PresignAssignmentUploadEndpoint
{
    public static void MapPresignAssignmentUploadEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/assignments/{assignmentId:guid}/presign", async (
                Guid assignmentId,
                PresignAssignmentUploadRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new PresignAssignmentUploadCommand
                {
                    AssignmentId = assignmentId,
                    FileName = request.FileName,
                    ContentType = request.ContentType
                };
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("PresignAssignmentUpload")
            .WithSummary("Generate 15-minute presigned S3 upload URL for student assignment submissions.")
            .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}

public sealed record PresignAssignmentUploadRequest(string FileName, string? ContentType);
