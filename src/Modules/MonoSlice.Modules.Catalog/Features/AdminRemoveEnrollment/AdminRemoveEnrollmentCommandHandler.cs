using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Catalog.Features.AdminRemoveEnrollment;

public sealed class AdminRemoveEnrollmentCommandHandler
    : ICommandHandler<AdminRemoveEnrollmentCommand, ApiResponse<bool>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEventStreamPublisher _eventStreamPublisher;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AdminRemoveEnrollmentCommandHandler> _logger;

    public AdminRemoveEnrollmentCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IEventStreamPublisher eventStreamPublisher,
        IEventBus eventBus,
        ILogger<AdminRemoveEnrollmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _eventStreamPublisher = eventStreamPublisher;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        AdminRemoveEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

        if (course is null)
        {
            return ApiResponse.Fail<bool>("Course not found.", 404);
        }

        var isAdmin = _currentUser.Roles.Contains("Admin");
        var isInstructor = _currentUser.Roles.Contains("Instructor");

        if (!isAdmin && (!isInstructor || course.InstructorId != _currentUser.UserId.Value))
        {
            return ApiResponse.Fail<bool>(
                "You are not authorized to manage enrollments for this course.", 403);
        }

        var enrollment = await _dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId && e.CourseId == request.CourseId, cancellationToken);

        if (enrollment is null)
        {
            return ApiResponse.Fail<bool>("Enrollment record not found.", 404);
        }

        var studentId = enrollment.UserId;

        // 1. Remove student's lesson progress for this course
        var progresses = await _dbContext.LessonProgresses
            .Where(lp => lp.CourseId == request.CourseId && lp.UserId == studentId)
            .ToListAsync(cancellationToken);

        if (progresses.Any())
        {
            _dbContext.LessonProgresses.RemoveRange(progresses);
        }

        // 2. Remove student's assignment submissions for this course
        var assignmentIds = await _dbContext.Assignments
            .Where(a => a.CourseId == request.CourseId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (assignmentIds.Any())
        {
            var assignmentSubmissions = await _dbContext.Submissions
                .Where(s => assignmentIds.Contains(s.AssignmentId) && s.StudentId == studentId)
                .ToListAsync(cancellationToken);

            if (assignmentSubmissions.Any())
            {
                _dbContext.Submissions.RemoveRange(assignmentSubmissions);
            }
        }

        // 3. Collect course exam IDs before removing enrollment
        var examIds = await _dbContext.CourseExams
            .Where(ce => ce.CourseId == request.CourseId)
            .Select(ce => ce.ExamId)
            .ToListAsync(cancellationToken);

        // 4. Remove enrollment
        _dbContext.Enrollments.Remove(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Publish StudentUnenrolledIntegrationEvent to EventStream and EventBus
        var integrationEvent = new StudentUnenrolledIntegrationEvent(
            request.CourseId,
            studentId,
            enrollment.Id,
            examIds,
            DateTime.UtcNow);

        try
        {
            await _eventStreamPublisher.PublishAsync(
                "stream:course-events",
                integrationEvent,
                ct: cancellationToken);

            await _eventBus.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Published StudentUnenrolledIntegrationEvent to EventStream for Student {UserId} in Course {CourseId} with {ExamCount} linked exams.",
                studentId, request.CourseId, examIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish StudentUnenrolledIntegrationEvent for Student {UserId} in Course {CourseId}.",
                studentId, request.CourseId);
        }

        return ApiResponse.Ok(true, "Student un-enrolled from course successfully.");
    }
}
