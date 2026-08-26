using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Exams.Features.GetExamSubmissions;

public record ViolationSummaryDto(string Type, string Reason, DateTime TimestampUtc);

public record ExamSubmissionDto(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string? StudentPicture,
    int AttemptNumber,
    int MaxAttempts,
    DateTime StartedAtUtc,
    DateTime? SubmittedAtUtc,
    string Status,
    decimal? Score,
    bool? IsPassed,
    int ViolationsCount,
    List<ViolationSummaryDto> Violations,
    int SnapshotsCount);

public sealed partial class GetExamSubmissionsQuery : IQuery<ApiResponse<PaginatedList<ExamSubmissionDto>>>
{
    [Required]
    public Guid ExamId { get; init; }

    public Guid? StudentId { get; init; }
    public string? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = 1;

    [Range(1, 1000)]
    public int PageSize { get; init; } = 20;
}
