using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.GetCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.AttachExam;

public sealed class AttachExamCommandHandler : ICommandHandler<AttachExamCommand, ApiResponse<CourseExamDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;

    public AttachExamCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<CourseExamDto>> Handle(
        AttachExamCommand command,
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

        var courseExam = course.AttachExam(command.ExamId, command.IsMandatory);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"course:{course.Id}", cancellationToken);

        var dto = new CourseExamDto(
            courseExam.Id,
            courseExam.ExamId,
            courseExam.OrderIndex,
            courseExam.IsMandatory);

        return ApiResponse.Ok(dto, "Exam attached to course successfully.");
    }
}
