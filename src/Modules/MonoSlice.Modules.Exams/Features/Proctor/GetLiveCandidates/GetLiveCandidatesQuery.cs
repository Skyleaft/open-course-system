using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;

public sealed record GetLiveCandidatesQuery(Guid ExamId) : IQuery<ApiResponse<IReadOnlyList<LiveCandidateDto>>>;

public sealed record LiveCandidateViolationDto(
    string ViolationType,
    string? Details,
    DateTime TimestampUtc);

public sealed record LiveCandidateDto(
    Guid SubmissionId,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string? StudentAvatarUrl,
    string Status,
    bool IsOnline,
    int ViolationCount,
    IReadOnlyList<LiveCandidateViolationDto> Violations,
    string? LatestSnapshotPresignedUrl,
    DateTime? LatestSnapshotTimeUtc,
    long RemainingSeconds,
    DateTime StartedAtUtc,
    DateTime MaxAllowedEndTimeUtc,
    int SnapshotsCaptured);

