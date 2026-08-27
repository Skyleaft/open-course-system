using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Catalog.Features.PresignCourseThumbnail;

public sealed class PresignCourseThumbnailCommandHandler : ICommandHandler<PresignCourseThumbnailCommand, ApiResponse<PresignedCourseThumbnailDto>>
{
    private const string BucketName = "branding-assets";
    private readonly ICurrentUser _currentUser;
    private readonly IObjectStorageService _storageService;

    public PresignCourseThumbnailCommandHandler(
        ICurrentUser currentUser,
        IObjectStorageService storageService)
    {
        _currentUser = currentUser;
        _storageService = storageService;
    }

    public async ValueTask<ApiResponse<PresignedCourseThumbnailDto>> Handle(
        PresignCourseThumbnailCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to upload course thumbnails.");
        }

        var sanitizedFileName = Path.GetFileName(command.FileName);
        var extension = Path.GetExtension(sanitizedFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var key = $"courses/thumbnails/{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.CreateVersion7():N}{extension}";
        var expiry = TimeSpan.FromMinutes(15);
        var contentType = string.IsNullOrWhiteSpace(command.ContentType) ? "image/jpeg" : command.ContentType;

        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(
            BucketName,
            key,
            expiry,
            contentType);

        var downloadUrl = await _storageService.GeneratePresignedDownloadUrlAsync(
            BucketName,
            key,
            TimeSpan.FromDays(365));

        var result = new PresignedCourseThumbnailDto(
            StorageKey: key,
            UploadUrl: uploadUrl,
            DownloadUrl: downloadUrl,
            ExpiresAtUtc: DateTime.UtcNow.Add(expiry));

        return ApiResponse.Ok(result, "Presigned course thumbnail upload URL generated.");
    }
}
