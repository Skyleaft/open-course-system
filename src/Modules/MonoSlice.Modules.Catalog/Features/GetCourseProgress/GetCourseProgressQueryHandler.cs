using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Catalog.Features.GetCourseProgress;

public sealed class GetCourseProgressQueryHandler : IQueryHandler<GetCourseProgressQuery, ApiResponse<CourseProgressDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IExamsModuleApi _examsModuleApi;

    public GetCourseProgressQueryHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IExamsModuleApi examsModuleApi)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _examsModuleApi = examsModuleApi;
    }

    public async ValueTask<ApiResponse<CourseProgressDto>> Handle(
        GetCourseProgressQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to view course progression.");
        }

        var userId = _currentUser.UserId.Value;

        var course = await _dbContext.Courses
            .AsNoTracking()
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Assignments)
            .Include(c => c.Exams)
            .FirstOrDefaultAsync(c => c.Id == query.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), query.CourseId);
        }

        var allLessons = course.Sections.SelectMany(s => s.Lessons).ToList();
        var totalLessons = allLessons.Count;
        var totalAssignments = course.Assignments.Count;
        var totalExams = course.Exams.Count;
        var totalItems = totalLessons + totalAssignments + totalExams;

        // Fetch completed lessons
        var lessonProgresses = await _dbContext.LessonProgresses
            .AsNoTracking()
            .Where(lp => lp.UserId == userId && lp.CourseId == query.CourseId && lp.IsCompleted)
            .ToListAsync(cancellationToken);

        var completedLessonIds = lessonProgresses.Select(lp => lp.LessonId).ToList();

        // Fetch completed assignments
        var assignmentIds = course.Assignments.Select(a => a.Id).ToList();
        var completedAssignmentIds = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId))
            .Select(s => s.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Fetch completed course exams
        var examIds = course.Exams.Select(e => e.ExamId).ToList();
        var examSubmissions = await _examsModuleApi.GetStudentSubmissionsForExamsAsync(userId, examIds, cancellationToken);
        var completedExamIds = examSubmissions
            .Where(s => string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.QuizId)
            .Distinct()
            .ToList();

        var completedCount = completedLessonIds.Count + completedAssignmentIds.Count + completedExamIds.Count;
        var progressPercent = totalItems > 0
            ? Math.Round(((decimal)completedCount / totalItems) * 100m, 1)
            : 0m;

        var lastAccessed = await _dbContext.LessonProgresses
            .AsNoTracking()
            .Where(lp => lp.UserId == userId && lp.CourseId == query.CourseId)
            .OrderByDescending(lp => lp.LastAccessedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var lastAccessedLessonId = lastAccessed?.LessonId ?? allLessons.OrderBy(l => l.OrderIndex).FirstOrDefault()?.Id;

        var dto = new CourseProgressDto(
            course.Id,
            completedLessonIds,
            completedAssignmentIds,
            completedExamIds,
            progressPercent,
            lastAccessedLessonId);

        return ApiResponse.Ok(dto);
    }
}
