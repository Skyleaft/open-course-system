using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Analytics.GetSecurityViolationsSummary;

public sealed partial class GetSecurityViolationsSummaryQuery : IQuery<ApiResponse<SecurityViolationsSummaryDto>>
{
}

public sealed class SecurityViolationsSummaryDto
{
    public int TotalSubmissions { get; init; }
    public int TotalViolations { get; init; }
    public int DisqualifiedCount { get; init; }
    public double DisqualificationRate { get; init; }
    public List<ViolationTypeCountDto> ViolationTypes { get; init; } = [];
    public List<HighRiskExamDto> HighRiskExams { get; init; } = [];
}

public sealed class ViolationTypeCountDto
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

public sealed class HighRiskExamDto
{
    public Guid ExamId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public int TotalAttempts { get; init; }
    public int ViolationsCount { get; init; }
    public int DisqualifiedCount { get; init; }
}
