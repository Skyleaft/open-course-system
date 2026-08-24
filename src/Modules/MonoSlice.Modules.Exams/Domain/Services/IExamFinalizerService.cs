using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.SubmitExam;

namespace MonoSlice.Modules.Exams.Domain.Services;

public interface IExamFinalizerService
{
    Task<ExamFinalResultDto> FinalizeAndGradeSubmissionAsync(
        Guid submissionId,
        SubmissionStatus targetStatus = SubmissionStatus.Completed,
        string? disqualificationReason = null,
        CancellationToken ct = default);
}
