using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Catalog.Features.DeleteCourse;

public sealed class DeleteCourseCommandHandler : ICommandHandler<DeleteCourseCommand, ApiResponse>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DeleteCourseCommandHandler> _logger;

    public DeleteCourseCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser,
        IEventBus eventBus,
        IObjectStorageService storageService,
        ILogger<DeleteCourseCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _eventBus = eventBus;
        _storageService = storageService;
        _logger = logger;
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

        // Clean up course thumbnail from MinIO object storage if present using global storage helper
        if (!string.IsNullOrWhiteSpace(course.ThumbnailUrl))
        {
            try
            {
                await _storageService.DeleteObjectByUrlAsync(course.ThumbnailUrl, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete thumbnail object from MinIO for URL '{ThumbnailUrl}'", course.ThumbnailUrl);
            }
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
