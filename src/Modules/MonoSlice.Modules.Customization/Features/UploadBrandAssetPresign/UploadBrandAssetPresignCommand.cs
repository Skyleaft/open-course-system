using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Customization.Features.UploadBrandAssetPresign;

public sealed record UploadBrandAssetPresignCommand(
    string FileName,
    string ContentType) : ICommand<ApiResponse<BrandAssetPresignDto>>;

public sealed class UploadBrandAssetPresignCommandHandler : ICommandHandler<UploadBrandAssetPresignCommand, ApiResponse<BrandAssetPresignDto>>
{
    private const string BucketName = "branding-assets";
    private readonly IObjectStorageService _storageService;

    public UploadBrandAssetPresignCommandHandler(IObjectStorageService storageService)
    {
        _storageService = storageService;
    }

    public async ValueTask<ApiResponse<BrandAssetPresignDto>> Handle(
        UploadBrandAssetPresignCommand command,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(command.FileName);
        var objectKey = $"branding/{Guid.NewGuid()}{extension}";
        var expiry = TimeSpan.FromMinutes(15);

        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(
            BucketName,
            objectKey,
            expiry,
            command.ContentType);

        var downloadUrl = await _storageService.GeneratePresignedDownloadUrlAsync(
            BucketName,
            objectKey,
            TimeSpan.FromDays(7));

        var result = new BrandAssetPresignDto(
            Bucket: BucketName,
            ObjectKey: objectKey,
            UploadUrl: uploadUrl,
            DownloadUrl: downloadUrl);

        return ApiResponse.Ok(result, "Presigned upload URL generated successfully.");
    }
}

public sealed record BrandAssetPresignDto(
    string Bucket,
    string ObjectKey,
    string UploadUrl,
    string DownloadUrl);
