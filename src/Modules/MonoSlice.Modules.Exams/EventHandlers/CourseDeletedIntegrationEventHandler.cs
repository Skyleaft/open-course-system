using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Exams.EventHandlers;

public sealed class CourseDeletedIntegrationEventHandler : IIntegrationEventHandler<CourseDeletedIntegrationEvent>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ILogger<CourseDeletedIntegrationEventHandler> _logger;

    public CourseDeletedIntegrationEventHandler(
        ExamsDbContext dbContext,
        ILogger<CourseDeletedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(CourseDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "CourseDeletedIntegrationEvent received for Course {CourseId}. Cascading cleanup of exams.",
            @event.CourseId);

        var exams = await _dbContext.Exams
            .Include(e => e.Questions)
            .Where(e => e.CourseId == @event.CourseId)
            .ToListAsync(cancellationToken);

        if (exams.Count > 0)
        {
            var examIds = exams.Select(e => e.Id).ToList();
            var submissions = await _dbContext.Submissions
                .Where(s => examIds.Contains(s.ExamId))
                .ToListAsync(cancellationToken);

            if (submissions.Count > 0)
            {
                _dbContext.Submissions.RemoveRange(submissions);
            }

            _dbContext.Exams.RemoveRange(exams);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {ExamCount} exams and {SubmissionCount} submissions for deleted Course {CourseId}.",
                exams.Count, submissions.Count, @event.CourseId);
        }
    }
}
