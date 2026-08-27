using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.Analytics.GetCourseAnalytics;

public sealed class GetCourseAnalyticsQueryHandler : IQueryHandler<GetCourseAnalyticsQuery, ApiResponse<CourseAnalyticsDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetCourseAnalyticsQueryHandler(
        CoursesDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<CourseAnalyticsDto>> Handle(GetCourseAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .AsNoTracking()
            .Include(c => c.Sections)
                .ThenInclude(s => s.Lessons)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == query.CourseId, cancellationToken);

        if (course is null)
        {
            return ApiResponse.Fail<CourseAnalyticsDto>("Course not found.", 404);
        }

        var cacheKey = $"cache:dashboard:instructor:courses:{query.CourseId}:analytics";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var enrollments = await _dbContext.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == query.CourseId)
                .ToListAsync(cancellationToken);

            var enrolledUserIds = enrollments.Select(e => e.UserId).Distinct().ToList();
            var totalEnrolled = enrolledUserIds.Count;

            var allLessonIds = course.Sections.SelectMany(s => s.Lessons).Select(l => l.Id).ToList();
            var totalLessons = allLessonIds.Count;

            var assignmentIds = course.Assignments.Select(a => a.Id).ToList();
            var totalAssignments = assignmentIds.Count;

            // Pending assignment reviews
            var pendingReviews = await _dbContext.Submissions
                .AsNoTracking()
                .CountAsync(s => assignmentIds.Contains(s.AssignmentId) && s.Score == null, cancellationToken);

            // Lesson progresses
            var lessonProgresses = await _dbContext.LessonProgresses
                .AsNoTracking()
                .Where(p => allLessonIds.Contains(p.LessonId) && enrolledUserIds.Contains(p.UserId) && p.IsCompleted)
                .ToListAsync(cancellationToken);

            // Students who completed all lessons
            int completedStudents = 0;
            if (totalLessons > 0)
            {
                var userCompletedLessonCounts = lessonProgresses
                    .GroupBy(p => p.UserId)
                    .Select(g => new { UserId = g.Key, CompletedLessons = g.Select(x => x.LessonId).Distinct().Count() })
                    .ToList();

                completedStudents = userCompletedLessonCounts.Count(u => u.CompletedLessons >= totalLessons);
            }

            double completionRate = totalEnrolled > 0 ? Math.Round((double)completedStudents / totalEnrolled * 100, 2) : 0.0;

            // Section drop-offs
            var sectionDropOffs = new List<SectionDropOffDto>();
            foreach (var sec in course.Sections.OrderBy(s => s.OrderIndex))
            {
                var secLessonIds = sec.Lessons.Select(l => l.Id).ToList();
                int studentsCompletedSec = 0;

                if (secLessonIds.Count > 0 && totalEnrolled > 0)
                {
                    var userCompletedInSec = lessonProgresses
                        .Where(p => secLessonIds.Contains(p.LessonId))
                        .GroupBy(p => p.UserId)
                        .Count(g => g.Select(x => x.LessonId).Distinct().Count() >= secLessonIds.Count);

                    studentsCompletedSec = userCompletedInSec;
                }
                else if (totalEnrolled > 0)
                {
                    studentsCompletedSec = totalEnrolled;
                }

                double retentionRate = totalEnrolled > 0 ? Math.Round((double)studentsCompletedSec / totalEnrolled * 100, 2) : 100.0;

                sectionDropOffs.Add(new SectionDropOffDto
                {
                    SectionId = sec.Id,
                    SectionTitle = sec.Title,
                    OrderIndex = sec.OrderIndex,
                    StudentsCompletedCount = studentsCompletedSec,
                    RetentionRate = retentionRate
                });
            }

            return new CourseAnalyticsDto
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                TotalEnrolled = totalEnrolled,
                CompletedStudentsCount = completedStudents,
                CompletionRate = completionRate,
                TotalSections = course.Sections.Count,
                TotalLessons = totalLessons,
                TotalAssignments = totalAssignments,
                PendingAssignmentReviewsCount = pendingReviews,
                SectionDropOffs = sectionDropOffs
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
