using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Catalog.Features.DeleteCourse;

public sealed class DeleteCourseCommandHandler : ICommandHandler<DeleteCourseCommand, ApiResponse>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    public DeleteCourseCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser,
        IEventBus eventBus)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    public async ValueTask<ApiResponse> Handle(
        DeleteCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Sections)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.Id);
        }

        if (!_currentUser.IsInRole("Admin") && _currentUser.UserId != course.InstructorId)
        {
            throw new ForbiddenException("You are not authorized to delete this course.");
        }

        // Remove any associated enrollments
        var enrollments = await _dbContext.Enrollments
            .Where(e => e.CourseId == course.Id)
            .ToListAsync(cancellationToken);
        if (enrollments.Count > 0)
        {
            _dbContext.Enrollments.RemoveRange(enrollments);
        }

        _dbContext.Courses.Remove(course);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate Redis caches
        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        // Asynchronously publish CourseDeletedIntegrationEvent to trigger cleanup across other modules
        var integrationEvent = new CourseDeletedIntegrationEvent(course.Id, course.InstructorId);
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        return ApiResponse.Ok("Course deleted successfully.");
    }
}
