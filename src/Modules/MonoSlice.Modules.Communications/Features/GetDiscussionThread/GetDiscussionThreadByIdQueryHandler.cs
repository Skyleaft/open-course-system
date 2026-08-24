using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Communications.Features.GetDiscussionThread;

public sealed class GetDiscussionThreadByIdQueryHandler : IQueryHandler<GetDiscussionThreadByIdQuery, ApiResponse<DiscussionThreadDetailDto>>
{
    private readonly CommunicationsDbContext _dbContext;

    public GetDiscussionThreadByIdQueryHandler(CommunicationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<DiscussionThreadDetailDto>> Handle(
        GetDiscussionThreadByIdQuery query,
        CancellationToken cancellationToken)
    {
        var thread = await _dbContext.DiscussionThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);

        if (thread is null)
        {
            throw new NotFoundException("DiscussionThread", query.Id);
        }

        var comments = await _dbContext.ThreadComments
            .AsNoTracking()
            .Where(c => c.ThreadId == query.Id)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var commentDtos = BuildCommentTree(comments);

        var dto = new DiscussionThreadDetailDto(
            thread.Id,
            thread.CourseId,
            thread.LessonId,
            thread.AuthorId,
            thread.Title,
            thread.Content,
            thread.IsClosed,
            thread.CreatedAtUtc,
            thread.ClosedAtUtc,
            thread.ClosedByUserId,
            commentDtos);

        return ApiResponse.Ok(dto);
    }

    private static List<ThreadCommentDto> BuildCommentTree(List<ThreadComment> comments)
    {
        var lookup = comments.ToLookup(c => c.ParentCommentId);

        List<ThreadCommentDto> GetReplies(Guid? parentId)
        {
            var replies = new List<ThreadCommentDto>();
            foreach (var item in lookup[parentId])
            {
                var children = GetReplies(item.Id);
                replies.Add(new ThreadCommentDto(
                    item.Id,
                    item.ThreadId,
                    item.AuthorId,
                    item.ParentCommentId,
                    item.Content,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    children));
            }
            return replies;
        }

        return GetReplies(null);
    }
}
