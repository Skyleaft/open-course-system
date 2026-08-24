using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.DetachExam;

public sealed class DetachExamCommandHandler : ICommandHandler<DetachExamCommand, ApiResponse<bool>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;

    public DetachExamCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        DetachExamCommand command,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Exams)
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.CourseId);
        }

        if (course.InstructorId != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
        {
            throw new UnauthorizedException("You are not authorized to modify this course.");
        }

        course.DetachExam(command.ExamId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        return ApiResponse.Ok(true, "Exam detached from course successfully.");
    }
}
