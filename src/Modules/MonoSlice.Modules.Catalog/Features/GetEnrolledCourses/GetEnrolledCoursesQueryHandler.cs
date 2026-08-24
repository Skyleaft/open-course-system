using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.GetEnrolledCourses;

public sealed class GetEnrolledCoursesQueryHandler : IQueryHandler<GetEnrolledCoursesQuery, ApiResponse<IReadOnlyList<EnrolledCourseDto>>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IExamsModuleApi _examsModuleApi;

    public GetEnrolledCoursesQueryHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IExamsModuleApi examsModuleApi)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _examsModuleApi = examsModuleApi;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<EnrolledCourseDto>>> Handle(
        GetEnrolledCoursesQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to view enrolled courses.");
        }

        var userId = _currentUser.UserId.Value;

        // 1. Fetch user's enrollments
        var enrollments = await _dbContext.Enrollments
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAtUtc)
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return ApiResponse.Ok<IReadOnlyList<EnrolledCourseDto>>([]);
        }

        var courseIds = enrollments.Select(e => e.CourseId).ToList();

        // 2. Fetch full course entities with sections, lessons, assignments, and exams
        var courses = await _dbContext.Courses
            .AsNoTracking()
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Assignments)
            .Include(c => c.Exams)
            .Where(c => courseIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        // 3. Fetch lesson progress for current user
        var lessonProgresses = await _dbContext.LessonProgresses
            .AsNoTracking()
            .Where(lp => lp.UserId == userId && courseIds.Contains(lp.CourseId))
            .ToListAsync(cancellationToken);

        // 4. Fetch assignment submissions for current user
        var assignmentIds = courses.SelectMany(c => c.Assignments.Select(a => a.Id)).ToList();
        var assignmentSubmissions = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId))
            .Select(s => s.AssignmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var completedAssignmentIdSet = assignmentSubmissions.ToHashSet();

        // 5. Fetch completed course exams for current user
        var allExamIds = courses.SelectMany(c => c.Exams.Select(e => e.ExamId)).Distinct().ToList();
        var examSubmissions = await _examsModuleApi.GetStudentSubmissionsForExamsAsync(userId, allExamIds, cancellationToken);
        var completedExamIdSet = examSubmissions
            .Where(s => string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.QuizId)
            .ToHashSet();

        var resultList = new List<EnrolledCourseDto>();

        foreach (var enrollment in enrollments)
        {
            var course = courses.FirstOrDefault(c => c.Id == enrollment.CourseId);
            if (course is null) continue;

            var allLessons = course.Sections.SelectMany(s => s.Lessons).ToList();
            var totalLessons = allLessons.Count;
            var totalAssignments = course.Assignments.Count;
            var totalExams = course.Exams.Count;

            var courseLessonProgress = lessonProgresses.Where(lp => lp.CourseId == course.Id).ToList();
            var completedLessons = courseLessonProgress.Count(lp => lp.IsCompleted);

            var courseCompletedAssignments = course.Assignments.Count(a => completedAssignmentIdSet.Contains(a.Id));
            var courseCompletedExams = course.Exams.Count(e => completedExamIdSet.Contains(e.ExamId));

            var totalItems = totalLessons + totalAssignments + totalExams;
            var completedItems = completedLessons + courseCompletedAssignments + courseCompletedExams;

            var progressPercent = totalItems > 0
                ? Math.Round(((decimal)completedItems / totalItems) * 100m, 1)
                : 0m;

            var lastAccessedProgress = courseLessonProgress
                .OrderByDescending(lp => lp.LastAccessedAtUtc)
                .FirstOrDefault();

            var lastAccessedLesson = lastAccessedProgress != null
                ? allLessons.FirstOrDefault(l => l.Id == lastAccessedProgress.LessonId)
                : allLessons.OrderBy(l => l.OrderIndex).FirstOrDefault();

            resultList.Add(new EnrolledCourseDto(
                course.Id,
                course.Title,
                course.Description,
                course.ThumbnailUrl,
                course.AccessType.ToString(),
                course.InstructorId,
                enrollment.EnrolledAtUtc,
                progressPercent,
                totalLessons,
                completedLessons,
                totalAssignments,
                courseCompletedAssignments,
                totalExams,
                courseCompletedExams,
                lastAccessedLesson?.Id,
                lastAccessedLesson?.Title));
        }

        return ApiResponse.Ok<IReadOnlyList<EnrolledCourseDto>>(resultList);
    }
}
