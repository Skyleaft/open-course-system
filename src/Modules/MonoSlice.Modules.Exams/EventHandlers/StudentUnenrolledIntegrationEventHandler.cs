using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Exams.EventHandlers;

public sealed class StudentUnenrolledIntegrationEventHandler : IIntegrationEventHandler<StudentUnenrolledIntegrationEvent>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StudentUnenrolledIntegrationEventHandler> _logger;

    public StudentUnenrolledIntegrationEventHandler(
        ExamsDbContext dbContext,
        ICacheService cacheService,
        ILogger<StudentUnenrolledIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task HandleAsync(StudentUnenrolledIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Consuming StudentUnenrolledIntegrationEvent: Clearing exam attempts & answer cache for Student {UserId} in Course {CourseId}.",
            @event.UserId, @event.CourseId);

        if (@event.ExamIds == null || !@event.ExamIds.Any())
        {
            return;
        }

        var submissions = await _dbContext.Submissions
            .Where(s => s.StudentId == @event.UserId && @event.ExamIds.Contains(s.ExamId))
            .ToListAsync(cancellationToken);

        if (submissions.Any())
        {
            foreach (var sub in submissions)
            {
                await _cacheService.RemoveAsync($"exam_answers:{sub.Id}", cancellationToken);
            }

            _dbContext.Submissions.RemoveRange(submissions);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully cleared {Count} exam attempt(s) for Student {UserId} linked to Course {CourseId}.",
                submissions.Count, @event.UserId, @event.CourseId);
        }
    }
}
