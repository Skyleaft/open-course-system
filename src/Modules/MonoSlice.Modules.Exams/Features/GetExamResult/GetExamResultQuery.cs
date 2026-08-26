using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetExamResult;

public sealed record GetExamResultQuery(Guid SubmissionId) : IQuery<ApiResponse<ExamResultDetailsDto>>;

public sealed record ExamResultDetailsDto(
    Guid SubmissionId,
    Guid ExamId,
    string ExamTitle,
    Guid? ExamRuleId,
    ExamRuleConfigDto AppliedRules,
    string Status,
    decimal? Score,
    bool? IsPassed,
    DateTime StartedAtUtc,
    DateTime? SubmittedAtUtc,
    IReadOnlyList<QuestionReviewDto> Questions);

public sealed record QuestionReviewDto(
    Guid QuestionId,
    string QuestionText,
    string Type,
    decimal Points,
    decimal? AwardedScore,
    IReadOnlyList<Guid> SelectedOptionIds,
    string? EssayText,
    string? Explanation,
    IReadOnlyList<OptionReviewDto> Options);

public sealed record OptionReviewDto(
    Guid Id,
    string Text,
    bool IsCorrect);
