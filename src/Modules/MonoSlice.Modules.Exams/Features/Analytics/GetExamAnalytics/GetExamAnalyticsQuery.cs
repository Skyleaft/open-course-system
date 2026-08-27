using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Analytics.GetExamAnalytics;

public sealed partial class GetExamAnalyticsQuery : IQuery<ApiResponse<ExamAnalyticsDto>>
{
    [Required]
    public Guid ExamId { get; init; }
}

public sealed class ExamAnalyticsDto
{
    public Guid ExamId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public int TotalSubmissions { get; init; }
    public int CompletedSubmissions { get; init; }
    public int DisqualifiedSubmissions { get; init; }
    public decimal AverageScore { get; init; }
    public decimal MedianScore { get; init; }
    public decimal HighestScore { get; init; }
    public decimal LowestScore { get; init; }
    public decimal StandardDeviation { get; init; }
    public decimal PassingScore { get; init; }
    public int PassedCount { get; init; }
    public int FailedCount { get; init; }
    public double PassRate { get; init; }
    public List<ScoreDistributionBucketDto> ScoreBuckets { get; init; } = [];
    public List<ItemPsychometricDto> ItemPsychometrics { get; init; } = [];
}

public sealed class ScoreDistributionBucketDto
{
    public string RangeLabel { get; init; } = string.Empty;
    public int MinScore { get; init; }
    public int MaxScore { get; init; }
    public int StudentCount { get; init; }
}

public sealed class ItemPsychometricDto
{
    public Guid QuestionId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string QuestionType { get; init; } = string.Empty;
    public decimal MaxPoints { get; init; }
    public int TotalAttempts { get; init; }
    public int CorrectCount { get; init; }
    public double DifficultyIndex { get; init; } // p-value
    public string DifficultyLabel { get; init; } = string.Empty; // Easy, Medium, Hard
    public double DiscriminationIndex { get; init; } // D-index
    public string DiscriminationStatus { get; init; } = string.Empty; // Excellent, Good, NeedsReview
}
