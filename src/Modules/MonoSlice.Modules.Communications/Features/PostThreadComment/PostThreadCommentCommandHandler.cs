using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Features.GetDiscussionThread;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Communications.Features.PostThreadComment;

public sealed class PostThreadCommentCommandHandler : ICommandHandler<PostThreadCommentCommand, ApiResponse<ThreadCommentDto>>
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public PostThreadCommentCommandHandler(
        CommunicationsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<ThreadCommentDto>> Handle(
        PostThreadCommentCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to post a comment.");
        }

        var thread = await _dbContext.DiscussionThreads
            .FirstOrDefaultAsync(t => t.Id == command.ThreadId, cancellationToken);

        if (thread is null)
        {
            throw new NotFoundException("DiscussionThread", command.ThreadId);
        }

        if (thread.IsClosed)
        {
            throw new BusinessRuleException("Cannot add comment to a closed discussion thread.");
        }

        if (command.ParentCommentId.HasValue)
        {
            var parentExists = await _dbContext.ThreadComments
                .AnyAsync(c => c.Id == command.ParentCommentId.Value && c.ThreadId == command.ThreadId, cancellationToken);

            if (!parentExists)
            {
                throw new BusinessRuleException("Parent comment does not exist in this discussion thread.");
            }
        }

        var comment = thread.AddComment(_currentUser.UserId.Value, command.Content, command.ParentCommentId);

        await _dbContext.ThreadComments.AddAsync(comment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ThreadCommentDto(
            comment.Id,
            comment.ThreadId,
            comment.AuthorId,
            comment.ParentCommentId,
            comment.Content,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc,
            []);

        return ApiResponse.Ok(dto, "Comment posted successfully.");
    }
}
