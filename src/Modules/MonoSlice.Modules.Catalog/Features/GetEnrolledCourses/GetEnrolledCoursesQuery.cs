using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.GetEnrolledCourses;

public sealed record GetEnrolledCoursesQuery : IQuery<ApiResponse<IReadOnlyList<EnrolledCourseDto>>>;

public sealed record EnrolledCourseDto(
    Guid Id,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    string AccessType,
    Guid InstructorId,
    DateTime EnrolledAtUtc,
    decimal ProgressPercent,
    int TotalLessonsCount,
    int CompletedLessonsCount,
    int TotalAssignmentsCount,
    int CompletedAssignmentsCount,
    int TotalExamsCount,
    int CompletedExamsCount,
    Guid? LastAccessedLessonId,
    string? LastAccessedLessonTitle);
