using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.AdminEnrollStudent;

public sealed record AdminEnrollStudentCommand(
    Guid CourseId,
    Guid? UserId,
    string? Email) : ICommand<ApiResponse<AdminEnrollStudentResultDto>>;

public sealed record AdminEnrollStudentResultDto(
    Guid EnrollmentId,
    Guid CourseId,
    Guid UserId,
    string StudentName,
    string StudentEmail,
    DateTime EnrolledAtUtc);
