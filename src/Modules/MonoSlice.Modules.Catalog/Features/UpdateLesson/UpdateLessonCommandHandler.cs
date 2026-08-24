using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.AddLesson;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.UpdateLesson;

public sealed class UpdateLessonCommandHandler : ICommandHandler<UpdateLessonCommand, ApiResponse<LessonResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;

    public UpdateLessonCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<LessonResultDto>> Handle(
        UpdateLessonCommand command,
        CancellationToken cancellationToken)
    {
        var lesson = await _dbContext.Lessons
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, cancellationToken);

        if (lesson is null)
        {
            throw new NotFoundException(nameof(Lesson), command.LessonId);
        }

        var section = await _dbContext.Sections
            .FirstOrDefaultAsync(s => s.Id == lesson.SectionId, cancellationToken);

        if (section is null)
        {
            throw new NotFoundException(nameof(CourseSection), lesson.SectionId);
        }

        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == section.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), section.CourseId);
        }

        if (!_currentUser.IsInRole("Admin") && _currentUser.UserId != course.InstructorId)
        {
            throw new ForbiddenException("You are not authorized to modify this lesson.");
        }

        lesson.Update(
            command.Title,
            command.Type,
            command.ContentUrl,
            command.DurationMinutes,
            command.TextContent,
            command.OrderIndex);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var result = new LessonResultDto(
            lesson.Id,
            lesson.SectionId,
            lesson.Title,
            lesson.Type.ToString(),
            lesson.ContentUrl,
            lesson.TextContent,
            lesson.DurationMinutes,
            lesson.OrderIndex);

        return ApiResponse.Ok(result, "Lesson updated successfully.");
    }
}
