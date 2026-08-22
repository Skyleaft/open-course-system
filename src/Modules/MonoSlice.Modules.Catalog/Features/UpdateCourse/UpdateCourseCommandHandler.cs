using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.UpdateCourse;

public sealed class UpdateCourseCommandHandler : ICommandHandler<UpdateCourseCommand, ApiResponse<CourseDetailDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public UpdateCourseCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<CourseDetailDto>> Handle(
        UpdateCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.Id);
        }

        string? keyHash = course.EnrollmentKeyHash;
        if (command.AccessType == CourseAccessType.PrivateWithKey && !string.IsNullOrWhiteSpace(command.EnrollmentKey))
        {
            keyHash = CreateCourseCommandHandler.HashKey(command.EnrollmentKey);
        }

        course.Update(
            command.Title,
            command.Description,
            command.AccessType,
            command.Price,
            keyHash,
            command.ThumbnailUrl);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate Redis cache
        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var dto = course.Adapt<CourseDetailDto>() with
        {
            AccessType = course.AccessType.ToString()
        };

        return ApiResponse.Ok(dto, "Course updated successfully.");
    }
}
