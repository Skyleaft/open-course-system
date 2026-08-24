using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetExamQuestions;

public sealed record GetExamQuestionsQuery(Guid SubmissionId) : IQuery<ApiResponse<StudentExamPaperDto>>;

public sealed record StudentExamPaperDto(
    Guid SubmissionId,
    Guid ExamId,
    string Title,
    string Mode,
    DateTime StartedAtUtc,
    DateTime MaxAllowedEndTimeUtc,
    string ActiveSessionToken,
    IReadOnlyList<StudentQuestionDto> Questions);

public sealed record StudentQuestionDto(
    Guid Id,
    string QuestionText,
    string Type,
    decimal Points,
    int DisplayOrder,
    IReadOnlyList<Guid>? SelectedOptionIds,
    string? EssayText,
    IReadOnlyList<StudentOptionDto> Options);

public sealed record StudentOptionDto(
    Guid Id,
    string Text);
