using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.SubmitExam;

public sealed record SubmitExamCommand(Guid SubmissionId) : ICommand<ApiResponse<ExamFinalResultDto>>;

public sealed record ExamFinalResultDto(
    Guid SubmissionId,
    Guid ExamId,
    string Status,
    decimal Score,
    bool IsPassed,
    DateTime SubmittedAtUtc,
    int TotalQuestions,
    int AnsweredQuestions);
