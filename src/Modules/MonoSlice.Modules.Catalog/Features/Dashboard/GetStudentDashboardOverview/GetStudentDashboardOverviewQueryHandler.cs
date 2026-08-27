using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.Dashboard.GetStudentDashboardOverview;

public sealed class GetStudentDashboardOverviewQueryHandler : IQueryHandler<GetStudentDashboardOverviewQuery, ApiResponse<StudentDashboardOverviewDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;

    public GetStudentDashboardOverviewQueryHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<StudentDashboardOverviewDto>> Handle(GetStudentDashboardOverviewQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return ApiResponse.Fail<StudentDashboardOverviewDto>("Unauthorized.", 401);
        }

        var studentId = userId.Value;
        var cacheKey = $"cache:dashboard:student:{studentId}:overview";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;

            // Enrolled courses
            var enrollments = await _dbContext.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == studentId)
                .ToListAsync(cancellationToken);

            var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();

            var courses = await _dbContext.Courses
                .AsNoTracking()
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
                .Include(c => c.Assignments)
                .Where(c => enrolledCourseIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            var allLessonIds = courses.SelectMany(c => c.Sections.SelectMany(s => s.Lessons)).Select(l => l.Id).ToList();

            var progresses = await _dbContext.LessonProgresses
                .AsNoTracking()
                .Where(p => p.UserId == studentId && allLessonIds.Contains(p.LessonId))
                .ToListAsync(cancellationToken);

            var completedLessonIds = progresses.Where(p => p.IsCompleted).Select(p => p.LessonId).ToHashSet();

            var studentCourses = new List<StudentCourseCardDto>();
            int completedCoursesCount = 0;

            foreach (var course in courses)
            {
                var courseLessons = course.Sections
                    .OrderBy(s => s.OrderIndex)
                    .SelectMany(s => s.Lessons.OrderBy(l => l.OrderIndex))
                    .ToList();

                int totalLessons = courseLessons.Count;
                int completedLessons = courseLessons.Count(l => completedLessonIds.Contains(l.Id));
                double progress = totalLessons > 0 ? Math.Round((double)completedLessons / totalLessons * 100, 1) : 0.0;

                if (progress >= 100.0)
                {
                    completedCoursesCount++;
                }

                // Next or last lesson
                var nextLesson = courseLessons.FirstOrDefault(l => !completedLessonIds.Contains(l.Id)) ?? courseLessons.LastOrDefault();

                studentCourses.Add(new StudentCourseCardDto
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    ThumbnailUrl = course.ThumbnailUrl,
                    AccessType = course.AccessType.ToString(),
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    ProgressPercentage = progress,
                    LastLessonId = nextLesson?.Id,
                    LastLessonTitle = nextLesson?.Title
                });
            }

            // Upcoming deadlines within 7 days
            var upcomingLimit = now.AddDays(7);
            var assignments = courses
                .SelectMany(c => c.Assignments.Select(a => new { Assignment = a, CourseTitle = c.Title }))
                .Where(x => x.Assignment.DeadlineUtc > now && x.Assignment.DeadlineUtc <= upcomingLimit)
                .OrderBy(x => x.Assignment.DeadlineUtc)
                .ToList();

            var assignmentSubmissions = await _dbContext.Submissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId && assignments.Select(a => a.Assignment.Id).Contains(s.AssignmentId))
                .Select(s => s.AssignmentId)
                .ToListAsync(cancellationToken);

            var submittedAssignmentIds = assignmentSubmissions.ToHashSet();

            var upcomingDeadlines = new List<UpcomingDeadlineItemDto>();
            foreach (var item in assignments)
            {
                if (submittedAssignmentIds.Contains(item.Assignment.Id))
                    continue;

                var remainingHours = (item.Assignment.DeadlineUtc - now).TotalHours;
                upcomingDeadlines.Add(new UpcomingDeadlineItemDto
                {
                    Id = item.Assignment.Id,
                    Title = item.Assignment.Title,
                    ItemType = "Assignment",
                    CourseTitle = item.CourseTitle,
                    DeadlineUtc = item.Assignment.DeadlineUtc,
                    RemainingHours = Math.Round(remainingHours, 1),
                    IsUrgent = remainingHours <= 24.0
                });
            }

            // Pending assignments count
            int pendingAssignments = upcomingDeadlines.Count;

            // Competency radar placeholder based on course areas
            var radarPoints = new List<CompetencyRadarPointDto>
            {
                new() { Subject = "Core Foundations", Value = Math.Min(100, Math.Max(20, (int)(studentCourses.Count * 25))), FullMark = 100 },
                new() { Subject = "Technical Skill", Value = Math.Min(100, Math.Max(30, (int)(completedCoursesCount * 40 + 20))), FullMark = 100 },
                new() { Subject = "Problem Solving", Value = Math.Min(100, Math.Max(25, (int)(studentCourses.Average(c => (double?)c.ProgressPercentage) ?? 30))), FullMark = 100 },
                new() { Subject = "Assessment Mastery", Value = Math.Min(100, Math.Max(35, (int)(completedLessonIds.Count * 5 + 25))), FullMark = 100 },
                new() { Subject = "Consistency", Value = Math.Min(100, Math.Max(40, (int)(progresses.Count * 4 + 30))), FullMark = 100 }
            };

            return new StudentDashboardOverviewDto
            {
                ActiveCoursesCount = studentCourses.Count,
                CompletedCoursesCount = completedCoursesCount,
                CertificatesCount = completedCoursesCount,
                PendingAssignmentsCount = pendingAssignments,
                EnrolledCourses = studentCourses,
                UpcomingDeadlines = upcomingDeadlines,
                CompetencyRadar = radarPoints
            };
        }, TimeSpan.FromMinutes(2), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
