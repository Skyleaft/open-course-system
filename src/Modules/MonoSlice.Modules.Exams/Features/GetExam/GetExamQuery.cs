using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetExam;

public sealed record GetExamQuery(Guid Id) : IQuery<ApiResponse<ExamFullDetailDto>>;

public sealed record QuizSectionDetailDto(
    Guid Id,
    Guid ExamId,
    Guid QuestionBankId,
    string? QuestionBankTitle,
    string Title,
    string? Description,
    int OrderIndex,
    decimal? PointsOverride,
    int? QuestionCount,
    IReadOnlyList<QuestionResultDto> Questions);

public sealed record ExamFullDetailDto(
    Guid Id,
    Guid InstructorId,
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
    bool ShuffleQuestions,
    bool ShuffleOptions,
    Guid CreatedBy,
    Guid? UpdatedBy,
    DateTime CreatedAtUtc,
    IReadOnlyList<QuizSectionDetailDto> Sections,
    IReadOnlyList<QuestionResultDto> Questions);
