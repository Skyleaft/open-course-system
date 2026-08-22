using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetExam;

public sealed record GetExamQuery(Guid Id) : IQuery<ApiResponse<ExamFullDetailDto>>;

public sealed record ExamFullDetailDto(
    Guid Id,
    Guid? CourseId,
    Guid InstructorId,
    string Title,
    string? Description,
    string Mode,
    int DurationMinutes,
    decimal PassingScore,
    int MaxAllowedViolations,
    bool IsPublished,
    bool ShuffleQuestions,
    bool ShuffleOptions,
    DateTime CreatedAtUtc,
    IReadOnlyList<QuestionResultDto> Questions);
