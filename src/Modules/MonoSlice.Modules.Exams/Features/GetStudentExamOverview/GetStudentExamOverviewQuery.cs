using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetStudentExamOverview;

public sealed record GetStudentExamOverviewQuery(Guid ExamId) : IQuery<ApiResponse<StudentExamOverviewDto>>;

public sealed record StudentExamOverviewDto(
    Guid Id,
    string Title,
    string? Description,
    string Mode,
    int DurationMinutes,
    decimal PassingScore,
    int MaxAllowedViolations,
    int MaxAttempts,
    DateTime? AvailableFromUtc,
    DateTime? AvailableToUtc,
    bool IsPublished,
    int TotalQuestionsCount,
    int SectionsCount,
    int CompletedAttemptsCount,
    int RemainingAttempts,
    decimal? BestScore,
    bool IsPassed,
    bool HasActiveSession,
    Guid? ActiveSubmissionId
);
