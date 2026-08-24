using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.AddLesson;

public sealed class AddLessonCommandHandler : ICommandHandler<AddLessonCommand, ApiResponse<LessonResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public AddLessonCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<LessonResultDto>> Handle(
        AddLessonCommand command,
        CancellationToken cancellationToken)
    {
        var section = await _dbContext.Sections
            .Include(s => s.Lessons)
            .FirstOrDefaultAsync(s => s.Id == command.SectionId, cancellationToken);

        if (section is null)
        {
            throw new NotFoundException(nameof(CourseSection), command.SectionId);
        }

        var lesson = section.AddLesson(
            command.Title,
            command.Type,
            command.ContentUrl,
            command.DurationMinutes,
            command.TextContent);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{section.CourseId}", cancellationToken);

        var result = new LessonResultDto(
            lesson.Id,
            lesson.SectionId,
            lesson.Title,
            lesson.Type.ToString(),
            lesson.ContentUrl,
            lesson.TextContent,
            lesson.DurationMinutes,
            lesson.OrderIndex);

        return ApiResponse.Ok(result, "Lesson added to section successfully.");
    }
}
