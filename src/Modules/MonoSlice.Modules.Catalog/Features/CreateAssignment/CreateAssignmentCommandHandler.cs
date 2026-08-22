using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.CreateAssignment;

public sealed class CreateAssignmentCommandHandler : ICommandHandler<CreateAssignmentCommand, ApiResponse<AssignmentResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public CreateAssignmentCommandHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<AssignmentResultDto>> Handle(
        CreateAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.CourseId);
        }

        var assignment = course.AddAssignment(
            command.Title,
            command.Instruction,
            command.DeadlineUtc,
            command.MaxScore);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var result = new AssignmentResultDto(
            assignment.Id,
            assignment.CourseId,
            assignment.Title,
            assignment.Instruction,
            assignment.DeadlineUtc,
            assignment.MaxScore);

        return ApiResponse.Ok(result, "Assignment created successfully.");
    }
}
