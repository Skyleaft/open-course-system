using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Customization.Features.UploadBrandAssetPresign;

public static class UploadBrandAssetPresignEndpoint
{
    public static void MapUploadBrandAssetPresignEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/assets/presign", async (
            UploadAssetPresignRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UploadBrandAssetPresignCommand(request.FileName, request.ContentType);
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UploadBrandAssetPresign")
        .WithSummary("Generate presigned MinIO S3 URL for brand assets (logos, favicons, banners).");
    }
}

public sealed record UploadAssetPresignRequest(string FileName, string ContentType);
