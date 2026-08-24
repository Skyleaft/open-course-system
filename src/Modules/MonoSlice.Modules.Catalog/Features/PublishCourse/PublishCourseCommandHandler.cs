using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.PublishCourse;

public sealed class PublishCourseCommandHandler : ICommandHandler<PublishCourseCommand, ApiResponse>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public PublishCourseCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse> Handle(
        PublishCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.Id);
        }

        if (command.Publish)
        {
            course.Publish();
        }
        else
        {
            course.Unpublish();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var action = command.Publish ? "published" : "unpublished";
        return ApiResponse.Ok($"Course '{course.Title}' {action} successfully.");
    }
}
