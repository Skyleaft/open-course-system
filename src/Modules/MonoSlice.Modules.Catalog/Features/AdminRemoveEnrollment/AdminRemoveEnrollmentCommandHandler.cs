using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.AdminRemoveEnrollment;

public sealed class AdminRemoveEnrollmentCommandHandler
    : ICommandHandler<AdminRemoveEnrollmentCommand, ApiResponse<bool>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AdminRemoveEnrollmentCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
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

        // Remove student's lesson progress for this course
        var progresses = await _dbContext.LessonProgresses
            .Where(lp => lp.CourseId == request.CourseId && lp.UserId == enrollment.UserId)
            .ToListAsync(cancellationToken);

        if (progresses.Any())
        {
            _dbContext.LessonProgresses.RemoveRange(progresses);
        }

        _dbContext.Enrollments.Remove(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(true, "Student un-enrolled from course successfully.");
    }
}
