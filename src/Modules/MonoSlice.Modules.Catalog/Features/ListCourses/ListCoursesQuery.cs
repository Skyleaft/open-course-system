using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.ListCourses;

public sealed record ListCoursesQuery : IQuery<ApiResponse<PaginatedList<CourseDetailDto>>>
{
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? AccessType { get; init; }
}
