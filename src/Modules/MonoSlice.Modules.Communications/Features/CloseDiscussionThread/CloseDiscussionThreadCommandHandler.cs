using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Communications.Features.CloseDiscussionThread;

public sealed class CloseDiscussionThreadCommandHandler : ICommandHandler<CloseDiscussionThreadCommand, ApiResponse<DiscussionThreadSummaryDto>>
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CloseDiscussionThreadCommandHandler(
        CommunicationsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<DiscussionThreadSummaryDto>> Handle(
        CloseDiscussionThreadCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to close a discussion thread.");
        }

        var thread = await _dbContext.DiscussionThreads
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == command.ThreadId, cancellationToken);

        if (thread is null)
        {
            throw new NotFoundException("DiscussionThread", command.ThreadId);
        }

        var isAuthor = thread.AuthorId == _currentUser.UserId.Value;
        var isPrivileged = _currentUser.Roles.Contains("Admin") || _currentUser.Roles.Contains("Instructor");

        if (!isAuthor && !isPrivileged)
        {
            throw new UnauthorizedAccessException("You are not authorized to close this discussion thread.");
        }

        thread.Close(_currentUser.UserId.Value);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new DiscussionThreadSummaryDto(
            thread.Id,
            thread.CourseId,
            thread.LessonId,
            thread.AuthorId,
            thread.Title,
            thread.Content,
            thread.IsClosed,
            thread.Comments.Count,
            thread.CreatedAtUtc,
            thread.ClosedAtUtc,
            thread.ClosedByUserId);

        return ApiResponse.Ok(dto, "Discussion thread closed successfully.");
    }
}
