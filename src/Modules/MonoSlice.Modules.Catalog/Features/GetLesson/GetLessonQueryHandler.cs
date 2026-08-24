using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.AddLesson;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Features.GetLesson;

public sealed class GetLessonQueryHandler : IQueryHandler<GetLessonQuery, ApiResponse<LessonResultDto>>
{
    private readonly CoursesDbContext _dbContext;

    public GetLessonQueryHandler(CoursesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<LessonResultDto>> Handle(
        GetLessonQuery query,
        CancellationToken cancellationToken)
    {
        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == query.LessonId, cancellationToken);

        if (lesson is null)
        {
            throw new NotFoundException(nameof(Lesson), query.LessonId);
        }

        var result = new LessonResultDto(
            lesson.Id,
            lesson.SectionId,
            lesson.Title,
            lesson.Type.ToString(),
            lesson.ContentUrl,
            lesson.TextContent,
            lesson.DurationMinutes,
            lesson.OrderIndex);

        return ApiResponse.Ok(result);
    }
}
