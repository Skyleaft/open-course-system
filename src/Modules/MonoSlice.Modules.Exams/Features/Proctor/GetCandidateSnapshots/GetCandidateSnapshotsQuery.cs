using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetCandidateSnapshots;

public sealed record GetCandidateSnapshotsQuery(Guid SubmissionId)
    : IQuery<ApiResponse<IReadOnlyList<CandidateSnapshotDto>>>;

public sealed record CandidateSnapshotDto(
    Guid Id,
    Guid SubmissionId,
    string StorageKey,
    string PresignedUrl,
    DateTime CapturedAtUtc);
