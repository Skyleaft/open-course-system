using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.PresignCourseThumbnail;

public static class PresignCourseThumbnailEndpoint
{
    public static void MapPresignCourseThumbnailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/thumbnails/presign", async (
                PresignCourseThumbnailRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new PresignCourseThumbnailCommand
                {
                    FileName = request.FileName,
                    ContentType = request.ContentType
                };
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("PresignCourseThumbnail")
            .WithSummary("Generate 15-minute presigned S3 upload URL for course thumbnail covers.")
            .RequireAuthorization(policy => policy.RequireRole("Instructor", "Admin"));
    }
}

public sealed record PresignCourseThumbnailRequest(string FileName, string? ContentType);
