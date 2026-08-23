using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.PresignSnapshot;

public sealed partial class PresignSnapshotCommand : ICommand<ApiResponse<PresignedSnapshotResultDto>>
{
    public Guid SubmissionId { get; init; }
    public string? ContentType { get; init; } = "image/jpeg";
}

public sealed record PresignedSnapshotResultDto(
    string StorageKey,
    string UploadUrl,
    DateTime ExpiresAtUtc);
