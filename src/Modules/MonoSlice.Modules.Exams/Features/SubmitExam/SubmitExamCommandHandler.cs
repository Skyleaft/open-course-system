using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.SubmitExam;

public sealed class SubmitExamCommandHandler : ICommandHandler<SubmitExamCommand, ApiResponse<ExamFinalResultDto>>
{
    private readonly IExamFinalizerService _finalizerService;
    private readonly ICurrentUser _currentUser;

    public SubmitExamCommandHandler(
        IExamFinalizerService finalizerService,
        ICurrentUser currentUser)
    {
        _finalizerService = finalizerService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ExamFinalResultDto>> Handle(
        SubmitExamCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var resultDto = await _finalizerService.FinalizeAndGradeSubmissionAsync(
            command.SubmissionId,
            SubmissionStatus.Completed,
            ct: cancellationToken);

        return ApiResponse.Ok(resultDto, "Exam submitted and graded successfully.");
    }
}
