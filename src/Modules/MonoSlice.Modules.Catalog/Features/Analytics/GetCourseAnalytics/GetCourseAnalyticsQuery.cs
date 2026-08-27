using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.Analytics.GetCourseAnalytics;

public sealed partial class GetCourseAnalyticsQuery : IQuery<ApiResponse<CourseAnalyticsDto>>
{
    [Required]
    public Guid CourseId { get; init; }
}

public sealed class CourseAnalyticsDto
{
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public int TotalEnrolled { get; init; }
    public int CompletedStudentsCount { get; init; }
    public double CompletionRate { get; init; }
    public int TotalSections { get; init; }
    public int TotalLessons { get; init; }
    public int TotalAssignments { get; init; }
    public int PendingAssignmentReviewsCount { get; init; }
    public List<SectionDropOffDto> SectionDropOffs { get; init; } = [];
}

public sealed class SectionDropOffDto
{
    public Guid SectionId { get; init; }
    public string SectionTitle { get; init; } = string.Empty;
    public int OrderIndex { get; init; }
    public int StudentsCompletedCount { get; init; }
    public double RetentionRate { get; init; }
}
