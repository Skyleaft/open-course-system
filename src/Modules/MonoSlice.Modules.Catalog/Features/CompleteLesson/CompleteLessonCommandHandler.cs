using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.CompleteLesson;

public sealed class CompleteLessonCommandHandler : ICommandHandler<CompleteLessonCommand, ApiResponse<LessonProgressResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CompleteLessonCommandHandler(CoursesDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<LessonProgressResultDto>> Handle(
        CompleteLessonCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to update lesson progress.");
        }

        var userId = _currentUser.UserId.Value;

        var course = await _dbContext.Courses
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Assignments)
            .Include(c => c.Exams)
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.CourseId);
        }

        var lessonExists = course.Sections.SelectMany(s => s.Lessons).Any(l => l.Id == command.LessonId);
        if (!lessonExists)
        {
            throw new NotFoundException(nameof(Lesson), command.LessonId);
        }

        var progress = await _dbContext.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.LessonId == command.LessonId, cancellationToken);

        var targetCompleted = command.IsCompleted ?? (progress is null || !progress.IsCompleted);

        if (progress is null)
        {
            progress = LessonProgress.Create(userId, command.CourseId, command.LessonId, targetCompleted);
            await _dbContext.LessonProgresses.AddAsync(progress, cancellationToken);
        }
        else
        {
            if (targetCompleted)
            {
                progress.MarkCompleted();
            }
            else
            {
                progress.MarkUncompleted();
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Calculate updated course progress percentage
        var allLessons = course.Sections.SelectMany(s => s.Lessons).ToList();
        var totalLessons = allLessons.Count;
        var totalAssignments = course.Assignments.Count;
        var totalExams = course.Exams.Count;
        var totalItems = totalLessons + totalAssignments + totalExams;

        var completedLessonsCount = await _dbContext.LessonProgresses
            .CountAsync(lp => lp.UserId == userId && lp.CourseId == command.CourseId && lp.IsCompleted, cancellationToken);

        var assignmentIds = course.Assignments.Select(a => a.Id).ToList();
        var completedAssignmentsCount = await _dbContext.Submissions
            .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId))
            .Select(s => s.AssignmentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var completedCount = completedLessonsCount + completedAssignmentsCount;
        var progressPercent = totalItems > 0
            ? Math.Round(((decimal)completedCount / totalItems) * 100m, 1)
            : 0m;

        var resultDto = new LessonProgressResultDto(
            command.CourseId,
            command.LessonId,
            progress.IsCompleted,
            progress.CompletedAtUtc,
            progressPercent);

        return ApiResponse.Ok(resultDto, targetCompleted ? "Lesson marked as completed." : "Lesson marked as uncompleted.");
    }
}
