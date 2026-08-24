using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Communications.Contracts;

public sealed class CommunicationsModuleApi : ICommunicationsModuleApi
{
    private readonly CommunicationsDbContext _dbContext;

    public CommunicationsModuleApi(CommunicationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetActiveAnnouncementsCountAsync(Guid? courseId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Announcements.AsNoTracking();

        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value || a.CourseId == null);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetDiscussionThreadsCountAsync(Guid courseId, Guid? lessonId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DiscussionThreads
            .AsNoTracking()
            .Where(t => t.CourseId == courseId);

        if (lessonId.HasValue)
        {
            query = query.Where(t => t.LessonId == lessonId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> IsThreadOpenAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.DiscussionThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

        return thread is not null && !thread.IsClosed;
    }
}
