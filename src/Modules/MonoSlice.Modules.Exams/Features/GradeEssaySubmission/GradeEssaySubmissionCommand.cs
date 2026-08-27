using MonoSlice.Modules.Exams.Features.GetExamResult;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GradeEssaySubmission;

public sealed partial class GradeEssaySubmissionCommand : ICommand<ApiResponse<ExamResultDetailsDto>>
{
    public Guid SubmissionId { get; init; }
    public List<EssayQuestionGradeDto> Grades { get; init; } = [];

    public GradeEssaySubmissionCommand() { }

    public GradeEssaySubmissionCommand(Guid submissionId, List<EssayQuestionGradeDto> grades)
    {
        SubmissionId = submissionId;
        Grades = grades;
    }
}

public sealed record EssayQuestionGradeDto(
    Guid QuestionId,
    decimal Score,
    string? Feedback = null
);
