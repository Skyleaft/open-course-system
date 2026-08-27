using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.Dashboard.GetStudentDashboardOverview;

public sealed partial class GetStudentDashboardOverviewQuery : IQuery<ApiResponse<StudentDashboardOverviewDto>>
{
}

public sealed class StudentDashboardOverviewDto
{
    public int ActiveCoursesCount { get; init; }
    public int CompletedCoursesCount { get; init; }
    public int CertificatesCount { get; init; }
    public int PendingAssignmentsCount { get; init; }
    public List<StudentCourseCardDto> EnrolledCourses { get; init; } = [];
    public List<UpcomingDeadlineItemDto> UpcomingDeadlines { get; init; } = [];
    public List<CompetencyRadarPointDto> CompetencyRadar { get; init; } = [];
}

public sealed class StudentCourseCardDto
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string AccessType { get; init; } = string.Empty;
    public int TotalLessons { get; init; }
    public int CompletedLessons { get; init; }
    public double ProgressPercentage { get; init; }
    public Guid? LastLessonId { get; init; }
    public string? LastLessonTitle { get; init; }
}

public sealed class UpcomingDeadlineItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ItemType { get; init; } = "Assignment"; // Assignment, Exam
    public string CourseTitle { get; init; } = string.Empty;
    public DateTime DeadlineUtc { get; init; }
    public double RemainingHours { get; init; }
    public bool IsUrgent { get; init; } // <= 24 hours
}

public sealed class CompetencyRadarPointDto
{
    public string Subject { get; init; } = string.Empty;
    public int Value { get; init; }
    public int FullMark { get; init; } = 100;
}
