using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.AdminRemoveEnrollment;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class AdminRemoveEnrollmentCommandHandlerTests
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEventStreamPublisher _eventStreamPublisher;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AdminRemoveEnrollmentCommandHandler> _logger;
    private readonly AdminRemoveEnrollmentCommandHandler _handler;

    public AdminRemoveEnrollmentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CoursesDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _eventStreamPublisher = Substitute.For<IEventStreamPublisher>();
        _eventBus = Substitute.For<IEventBus>();
        _logger = Substitute.For<ILogger<AdminRemoveEnrollmentCommandHandler>>();

        _handler = new AdminRemoveEnrollmentCommandHandler(
            _dbContext,
            _currentUser,
            _eventStreamPublisher,
            _eventBus,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenAdminRemovesEnrollment_ShouldDeleteProgressAndPublishEvent()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(adminId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        var course = Course.Create(
            instructorId,
            "Test Course",
            "Description",
            CourseAccessType.OpenFree);

        var section = course.AddSection("Section 1");
        var lesson = section.AddLesson("Lesson 1", LessonType.Video, "http://video.mp4", 10);
        _dbContext.Courses.Add(course);

        var examId = Guid.NewGuid();
        var courseExam = CourseExam.Create(course.Id, examId, 1, true);
        _dbContext.CourseExams.Add(courseExam);

        var enrollment = CourseEnrollment.Create(studentId, course.Id);
        _dbContext.Enrollments.Add(enrollment);

        var progress = LessonProgress.Create(studentId, course.Id, lesson.Id);
        _dbContext.LessonProgresses.Add(progress);

        await _dbContext.SaveChangesAsync();

        var command = new AdminRemoveEnrollmentCommand(course.Id, enrollment.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);

        var enrollmentInDb = await _dbContext.Enrollments.FindAsync(enrollment.Id);
        Assert.Null(enrollmentInDb);

        var progressInDb = await _dbContext.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.CourseId == course.Id && lp.UserId == studentId);
        Assert.Null(progressInDb);

        // Verify EventStream was published
        await _eventStreamPublisher.Received(1).PublishAsync(
            "stream:course-events",
            Arg.Is<StudentUnenrolledIntegrationEvent>(e =>
                e.CourseId == course.Id &&
                e.UserId == studentId &&
                e.EnrollmentId == enrollment.Id &&
                e.ExamIds.Contains(examId)),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());

        // Verify EventBus was published
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<StudentUnenrolledIntegrationEvent>(e =>
                e.CourseId == course.Id &&
                e.UserId == studentId),
            Arg.Any<CancellationToken>());
    }
}
