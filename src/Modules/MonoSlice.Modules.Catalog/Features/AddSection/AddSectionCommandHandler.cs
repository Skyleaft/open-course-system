using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.AddSection;

public sealed class AddSectionCommandHandler : ICommandHandler<AddSectionCommand, ApiResponse<SectionResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public AddSectionCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<SectionResultDto>> Handle(
        AddSectionCommand command,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Sections)
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.CourseId);
        }

        var section = course.AddSection(command.Title);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var result = new SectionResultDto(section.Id, section.CourseId, section.Title, section.OrderIndex);
        return ApiResponse.Ok(result, "Curriculum section added successfully.");
    }
}
