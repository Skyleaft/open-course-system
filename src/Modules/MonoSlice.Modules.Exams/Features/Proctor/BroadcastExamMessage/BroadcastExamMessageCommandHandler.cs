using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Hubs;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Features.Proctor.BroadcastExamMessage;

public sealed class BroadcastExamMessageCommandHandler
    : ICommandHandler<BroadcastExamMessageCommand, ApiResponse>
{
    private readonly ExamsDbContext _dbContext;
    private readonly IHubContext<ExamHub> _hubContext;

    public BroadcastExamMessageCommandHandler(
        ExamsDbContext dbContext,
        IHubContext<ExamHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async ValueTask<ApiResponse> Handle(
        BroadcastExamMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Message))
        {
            throw new BusinessRuleException("Broadcast message cannot be empty.");
        }

        var exam = await _dbContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.ExamId);
        }

        // Send room-wide announcement to all candidates in the exam room
        await _hubContext.Clients.Group($"exam_room_{command.ExamId}")
            .SendAsync("ProctorMessage", command.Message, cancellationToken);

        // Also notify any proctor monitors
        await _hubContext.Clients.Group($"proctor_exam_{command.ExamId}")
            .SendAsync("RoomBroadcastSent", command.Message, DateTime.UtcNow, cancellationToken);

        return ApiResponse.Ok("Broadcast announcement successfully sent to all candidates in room.");
    }
}
