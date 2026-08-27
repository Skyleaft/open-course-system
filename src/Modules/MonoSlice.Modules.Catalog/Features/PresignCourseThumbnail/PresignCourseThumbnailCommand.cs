using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.PresignCourseThumbnail;

public sealed partial class PresignCourseThumbnailCommand : ICommand<ApiResponse<PresignedCourseThumbnailDto>>
{
    [Required]
    public string FileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }
}

public sealed record PresignedCourseThumbnailDto(
    string StorageKey,
    string UploadUrl,
    string DownloadUrl,
    DateTime ExpiresAtUtc);
