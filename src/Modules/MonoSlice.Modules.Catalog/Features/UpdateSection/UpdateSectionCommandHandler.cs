using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.AddSection;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.UpdateSection;

public sealed class UpdateSectionCommandHandler : ICommandHandler<UpdateSectionCommand, ApiResponse<SectionResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;

    public UpdateSectionCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<SectionResultDto>> Handle(
        UpdateSectionCommand command,
        CancellationToken cancellationToken)
    {
        var section = await _dbContext.Sections
            .FirstOrDefaultAsync(s => s.Id == command.SectionId, cancellationToken);

        if (section is null)
        {
            throw new NotFoundException(nameof(CourseSection), command.SectionId);
        }

        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == section.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), section.CourseId);
        }

        if (!_currentUser.IsInRole("Admin") && _currentUser.UserId != course.InstructorId)
        {
            throw new ForbiddenException("You are not authorized to modify this curriculum section.");
        }

        section.Update(command.Title, command.OrderIndex);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var result = new SectionResultDto(section.Id, section.CourseId, section.Title, section.OrderIndex);
        return ApiResponse.Ok(result, "Curriculum section updated successfully.");
    }
}
