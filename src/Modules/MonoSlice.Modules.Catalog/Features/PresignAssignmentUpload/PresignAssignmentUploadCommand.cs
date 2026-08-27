using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.PresignAssignmentUpload;

public sealed partial class PresignAssignmentUploadCommand : ICommand<ApiResponse<PresignedAssignmentUploadDto>>
{
    public Guid AssignmentId { get; init; }

    [Required]
    public string FileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }
}

public sealed record PresignedAssignmentUploadDto(
    string StorageKey,
    string UploadUrl,
    DateTime ExpiresAtUtc);
