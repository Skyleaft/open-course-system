using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Hubs;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Features.Proctor.WarnCandidate;

public sealed class WarnCandidateCommandHandler : ICommandHandler<WarnCandidateCommand, ApiResponse>
{
    private readonly ExamsDbContext _dbContext;
    private readonly IHubContext<ExamHub> _hubContext;

    public WarnCandidateCommandHandler(
        ExamsDbContext dbContext,
        IHubContext<ExamHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async ValueTask<ApiResponse> Handle(
        WarnCandidateCommand command,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), command.SubmissionId);
        }

        if (submission.Status != SubmissionStatus.InProgress)
        {
            throw new BusinessRuleException($"Candidate submission is already {submission.Status}.");
        }

        // Send real-time modal warning via SignalR
        await _hubContext.Clients.Group($"exam_{command.SubmissionId}")
            .SendAsync("ProctorMessage", command.Message, cancellationToken);

        return ApiResponse.Ok("Warning successfully dispatched to candidate.");
    }
}
