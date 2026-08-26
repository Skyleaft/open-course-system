using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.EventHandlers;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class StudentUnenrolledIntegrationEventHandlerTests
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StudentUnenrolledIntegrationEventHandler> _logger;
    private readonly StudentUnenrolledIntegrationEventHandler _handler;

    public StudentUnenrolledIntegrationEventHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExamsDbContext(options);
        _cacheService = Substitute.For<ICacheService>();
        _logger = Substitute.For<ILogger<StudentUnenrolledIntegrationEventHandler>>();

        _handler = new StudentUnenrolledIntegrationEventHandler(
            _dbContext,
            _cacheService,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WhenStudentUnenrolled_ShouldRemoveSubmissionsAndClearRedisAnswerCache()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var examId1 = Guid.NewGuid();
        var examId2 = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var submission1 = QuizSubmission.Create(examId1, studentId, 60, 1234, Guid.NewGuid().ToString());
        var submission2 = QuizSubmission.Create(examId2, studentId, 60, 5678, Guid.NewGuid().ToString());

        _dbContext.Submissions.AddRange(submission1, submission2);
        await _dbContext.SaveChangesAsync();

        var @event = new StudentUnenrolledIntegrationEvent(
            courseId,
            studentId,
            Guid.NewGuid(),
            new List<Guid> { examId1, examId2 },
            DateTime.UtcNow);

        // Act
        await _handler.HandleAsync(@event, CancellationToken.None);

        // Assert
        var remaining = await _dbContext.Submissions
            .Where(s => s.StudentId == studentId)
            .ToListAsync();

        Assert.Empty(remaining);

        // Verify Redis cache removed for both submissions
        await _cacheService.Received(1).RemoveAsync($"exam_answers:{submission1.Id}", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync($"exam_answers:{submission2.Id}", Arg.Any<CancellationToken>());
    }
}
