using Sannr;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.CreateCourse;

public sealed partial class CreateCourseCommand : ICommand<ApiResponse<CourseDetailDto>>
{
    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public CourseAccessType AccessType { get; init; } = CourseAccessType.OpenFree;

    public decimal Price { get; init; } = 0m;

    public string? EnrollmentKey { get; init; }

    public string? ThumbnailUrl { get; init; }
}

public sealed record CourseDetailDto(
    Guid Id,
    Guid InstructorId,
    string Title,
    string? Description,
    string AccessType,
    decimal Price,
    bool IsPublished,
    string? ThumbnailUrl,
    DateTime CreatedAtUtc);
