using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.EnrollCourse;

public sealed record EnrollCourseCommand : ICommand<ApiResponse<EnrollmentResultDto>>
{
    public Guid CourseId { get; init; }
    public string? EnrollmentKey { get; init; }
}

public sealed record EnrollmentResultDto(
    Guid EnrollmentId,
    Guid UserId,
    Guid CourseId,
    DateTime EnrolledAtUtc);
