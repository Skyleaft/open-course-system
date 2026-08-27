using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetProctorLiveSummary;

public sealed partial class GetProctorLiveSummaryQuery : IQuery<ApiResponse<ProctorLiveSummaryDto>>
{
}

public sealed class ProctorLiveSummaryDto
{
    public int ActiveExamsCount { get; init; }
    public int ActiveExamineesCount { get; init; }
    public int HighRiskCandidatesCount { get; init; }
    public List<CandidateLiveRiskDto> FlaggedCandidates { get; init; } = [];
    public List<ActiveExamSummaryDto> ActiveExams { get; init; } = [];
}

public sealed class CandidateLiveRiskDto
{
    public Guid SubmissionId { get; init; }
    public Guid ExamId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public Guid StudentId { get; init; }
    public int ViolationsCount { get; init; }
    public double RiskScore { get; init; }
    public string RiskLevel { get; init; } = "Low"; // High, Medium, Low
    public DateTime StartedAtUtc { get; init; }
    public DateTime MaxAllowedEndTimeUtc { get; init; }
}

public sealed class ActiveExamSummaryDto
{
    public Guid ExamId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ActiveExaminees { get; init; }
}
