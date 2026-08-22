using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Assessments.Features.Admin.RedriveDeadLetter;

public sealed class RedriveDeadLetterCommandHandler : ICommandHandler<RedriveDeadLetterCommand, ApiResponse>
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly IEventStreamPublisher _eventPublisher;
    private readonly ICurrentUser _currentUser;

    public RedriveDeadLetterCommandHandler(
        AssessmentsDbContext dbContext,
        IEventStreamPublisher eventPublisher,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse> Handle(
        RedriveDeadLetterCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var deadLetter = await _dbContext.GradingDeadLetters
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (deadLetter is null)
        {
            throw new NotFoundException(nameof(GradingDeadLetter), command.Id);
        }

        if (!string.IsNullOrEmpty(deadLetter.PayloadJson))
        {
            var entries = new Dictionary<string, string>
            {
                ["id"] = Guid.CreateVersion7().ToString(),
                ["payload"] = deadLetter.PayloadJson,
                ["retry_count"] = "0",
                ["redrive_at_utc"] = DateTime.UtcNow.ToString("O")
            };

            await _eventPublisher.PublishRawAsync(
                "stream:exam-events",
                entries,
                ct: cancellationToken);
        }

        deadLetter.MarkResolved();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Dead letter re-driven to stream:exam-events and marked as resolved.");
    }
}
