using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Communications.EventHandlers;

public sealed class CourseDeletedIntegrationEventHandler : IIntegrationEventHandler<CourseDeletedIntegrationEvent>
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly ILogger<CourseDeletedIntegrationEventHandler> _logger;

    public CourseDeletedIntegrationEventHandler(
        CommunicationsDbContext dbContext,
        ILogger<CourseDeletedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(CourseDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "CourseDeletedIntegrationEvent received for Course {CourseId}. Cascading cleanup of communications.",
            @event.CourseId);

        var announcements = await _dbContext.Announcements
            .Where(a => a.CourseId == @event.CourseId)
            .ToListAsync(cancellationToken);

        if (announcements.Count > 0)
        {
            _dbContext.Announcements.RemoveRange(announcements);
        }

        var threads = await _dbContext.DiscussionThreads
            .Include(t => t.Comments)
            .Where(t => t.CourseId == @event.CourseId)
            .ToListAsync(cancellationToken);

        if (threads.Count > 0)
        {
            _dbContext.DiscussionThreads.RemoveRange(threads);
        }

        if (announcements.Count > 0 || threads.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Cleaned up {AnnouncementCount} announcements and {ThreadCount} threads for deleted Course {CourseId}.",
                announcements.Count, threads.Count, @event.CourseId);
        }
    }
}
