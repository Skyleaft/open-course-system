using Mapster;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Communications.Features.CreateDiscussionThread;

public sealed class CreateDiscussionThreadCommandHandler : ICommandHandler<CreateDiscussionThreadCommand, ApiResponse<DiscussionThreadSummaryDto>>
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateDiscussionThreadCommandHandler(
        CommunicationsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<DiscussionThreadSummaryDto>> Handle(
        CreateDiscussionThreadCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to start a discussion thread.");
        }

        var thread = DiscussionThread.Create(
            command.CourseId,
            command.LessonId,
            _currentUser.UserId.Value,
            command.Title,
            command.Content);

        await _dbContext.DiscussionThreads.AddAsync(thread, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new DiscussionThreadSummaryDto(
            thread.Id,
            thread.CourseId,
            thread.LessonId,
            thread.AuthorId,
            thread.Title,
            thread.Content,
            thread.IsClosed,
            0,
            thread.CreatedAtUtc,
            thread.ClosedAtUtc,
            thread.ClosedByUserId);

        return ApiResponse.Ok(dto, "Discussion thread created successfully.");
    }
}
