using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;

public sealed record GetLiveCandidatesQuery(Guid ExamId) : IQuery<ApiResponse<IReadOnlyList<LiveCandidateDto>>>;

public sealed record LiveCandidateDto(
    Guid SubmissionId,
    Guid StudentId,
    string Status,
    bool IsOnline,
    int ViolationCount,
    long RemainingSeconds,
    DateTime StartedAtUtc,
    DateTime MaxAllowedEndTimeUtc,
    int SnapshotsCaptured);
