using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.AdminRemoveEnrollment;

public sealed record AdminRemoveEnrollmentCommand(
    Guid CourseId,
    Guid EnrollmentId) : ICommand<ApiResponse<bool>>;
