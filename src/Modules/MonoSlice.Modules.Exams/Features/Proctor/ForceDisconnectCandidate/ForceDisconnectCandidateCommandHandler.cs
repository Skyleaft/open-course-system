using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Hubs;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.Proctor.ForceDisconnectCandidate;

public sealed class ForceDisconnectCandidateCommandHandler : ICommandHandler<ForceDisconnectCandidateCommand, ApiResponse>
{
    private readonly ExamsDbContext _dbContext;
    private readonly IHubContext<ExamHub> _hubContext;
    private readonly ICacheService _cacheService;

    public ForceDisconnectCandidateCommandHandler(
        ExamsDbContext dbContext,
        IHubContext<ExamHub> hubContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse> Handle(
        ForceDisconnectCandidateCommand command,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), command.SubmissionId);
        }

        submission.Disqualify(command.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Remove active session from Redis
        await _cacheService.RemoveAsync($"exam_session:{submission.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"exam_liveness:{submission.Id}", cancellationToken);

        // SignalR Disconnect broadcast
        await _hubContext.Clients.Group($"exam_{command.SubmissionId}")
            .SendAsync("ForceDisconnectExam", command.Reason, cancellationToken);

        await _hubContext.Clients.Group($"proctor_exam_{submission.ExamId}")
            .SendAsync("CandidateStatusChanged", submission.Id, "Disqualified", cancellationToken);

        return ApiResponse.Ok("Candidate forcibly disqualified and disconnected.");
    }
}
