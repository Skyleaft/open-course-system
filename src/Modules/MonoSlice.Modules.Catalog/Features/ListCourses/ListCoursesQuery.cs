using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Catalog.Features.ListCourses;

public sealed partial class ListCoursesQuery : IQuery<ApiResponse<PaginatedList<CourseDetailDto>>>
{
    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = 1;

    [Range(1, 1000)]
    public int PageSize { get; init; } = 10;

    public string? SearchTerm { get; init; }
    public string? AccessType { get; init; }
    public Guid? InstructorId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsPublished { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}
