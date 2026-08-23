using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.SubmitExam;

public sealed partial class SubmitExamCommand : ICommand<ApiResponse<ExamFinalResultDto>>
{
    public Guid SubmissionId { get; init; }
}

public sealed record ExamFinalResultDto(
    Guid SubmissionId,
    Guid ExamId,
    string Status,
    decimal Score,
    bool IsPassed,
    DateTime SubmittedAtUtc,
    int TotalQuestions,
    int AnsweredQuestions);
