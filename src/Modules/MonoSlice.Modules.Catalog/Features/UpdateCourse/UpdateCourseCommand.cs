using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.UpdateCourse;

public sealed record UpdateCourseCommand : ICommand<ApiResponse<CourseDetailDto>>
{
    public Guid Id { get; init; }

    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public CourseAccessType AccessType { get; init; } = CourseAccessType.OpenFree;

    public decimal Price { get; init; } = 0m;

    public string? EnrollmentKey { get; init; }

    public string? ThumbnailUrl { get; init; }
}
