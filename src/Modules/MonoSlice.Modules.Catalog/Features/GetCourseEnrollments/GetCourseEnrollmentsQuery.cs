using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.GetCourseEnrollments;

public sealed record GetCourseEnrollmentsQuery(
    Guid CourseId,
    int PageIndex = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<ApiResponse<PaginatedList<CourseStudentEnrollmentDto>>>;

public sealed record CourseStudentEnrollmentDto(
    Guid EnrollmentId,
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    DateTime EnrolledAtUtc,
    decimal ProgressPercent,
    int CompletedLessonsCount,
    int TotalLessonsCount,
    int CompletedAssignmentsCount,
    int TotalAssignmentsCount,
    DateTime? LastAccessedAtUtc);
