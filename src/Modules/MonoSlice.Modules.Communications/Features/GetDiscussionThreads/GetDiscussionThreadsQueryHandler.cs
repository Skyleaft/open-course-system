using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.GetDiscussionThreads;

public sealed class GetDiscussionThreadsQueryHandler : IQueryHandler<GetDiscussionThreadsQuery, ApiResponse<PaginatedList<DiscussionThreadSummaryDto>>>
{
    private readonly CommunicationsDbContext _dbContext;

    public GetDiscussionThreadsQueryHandler(CommunicationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<PaginatedList<DiscussionThreadSummaryDto>>> Handle(
        GetDiscussionThreadsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.DiscussionThreads.AsNoTracking();

        if (query.CourseId.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.CourseId == query.CourseId.Value);
        }

        if (query.LessonId.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.LessonId == query.LessonId.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var items = await dbQuery
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new DiscussionThreadSummaryDto(
                t.Id,
                t.CourseId,
                t.LessonId,
                t.AuthorId,
                t.Title,
                t.Content,
                t.IsClosed,
                t.Comments.Count,
                t.CreatedAtUtc,
                t.ClosedAtUtc,
                t.ClosedByUserId))
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<DiscussionThreadSummaryDto>(items, totalCount, pageNumber, pageSize);
        return ApiResponse.Ok(paginatedList);
    }
}
