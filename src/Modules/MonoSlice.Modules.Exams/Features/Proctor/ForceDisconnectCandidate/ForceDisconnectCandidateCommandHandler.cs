using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Hubs;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Features.Proctor.ForceDisconnectCandidate;

public sealed class ForceDisconnectCandidateCommandHandler : ICommandHandler<ForceDisconnectCandidateCommand, ApiResponse>
{
    private readonly ExamsDbContext _dbContext;
    private readonly IExamFinalizerService _finalizerService;
    private readonly IHubContext<ExamHub> _hubContext;

    public ForceDisconnectCandidateCommandHandler(
        ExamsDbContext dbContext,
        IExamFinalizerService finalizerService,
        IHubContext<ExamHub> hubContext)
    {
        _dbContext = dbContext;
        _finalizerService = finalizerService;
        _hubContext = hubContext;
    }

    public async ValueTask<ApiResponse> Handle(
        ForceDisconnectCandidateCommand command,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), command.SubmissionId);
        }

        // Flush answers and finalize submission as Disqualified
        await _finalizerService.FinalizeAndGradeSubmissionAsync(
            command.SubmissionId,
            SubmissionStatus.Disqualified,
            command.Reason,
            cancellationToken);

        // SignalR Disconnect broadcast
        await _hubContext.Clients.Group($"exam_{command.SubmissionId}")
            .SendAsync("ForceDisconnectExam", command.Reason, cancellationToken);

        await _hubContext.Clients.Group($"proctor_exam_{submission.ExamId}")
            .SendAsync("CandidateStatusChanged", submission.Id, "Disqualified", cancellationToken);

        return ApiResponse.Ok("Candidate forcibly disqualified and disconnected.");
    }
}
