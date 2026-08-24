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
    IReadOnlyList<StudentQuestionDto> Questions,
    IReadOnlyList<StudentExamSectionDto>? Sections = null);

public sealed record StudentExamSectionDto(
    Guid Id,
    string Title,
    string? Description,
    int OrderIndex,
    int QuestionCount);

public sealed record StudentQuestionDto(
    Guid Id,
    string QuestionText,
    string Type,
    decimal Points,
    int DisplayOrder,
    IReadOnlyList<Guid>? SelectedOptionIds,
    string? EssayText,
    IReadOnlyList<StudentOptionDto> Options,
    Guid? SectionId = null,
    string? SectionTitle = null);

public sealed record StudentOptionDto(
    Guid Id,
    string Text);
