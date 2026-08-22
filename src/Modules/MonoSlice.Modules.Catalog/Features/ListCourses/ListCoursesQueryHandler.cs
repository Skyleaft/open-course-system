using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.ListCourses;

public sealed class ListCoursesQueryHandler : IQueryHandler<ListCoursesQuery, ApiResponse<PaginatedList<CourseDetailDto>>>
{
    private readonly CoursesDbContext _dbContext;

    public ListCoursesQueryHandler(CoursesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<PaginatedList<CourseDetailDto>>> Handle(
        ListCoursesQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(c => c.Title.ToLower().Contains(search) || 
                                         (c.Description != null && c.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.AccessType) && Enum.TryParse<CourseAccessType>(query.AccessType, true, out var accessType))
        {
            dbQuery = dbQuery.Where(c => c.AccessType == accessType);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var courses = await dbQuery
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = courses.Select(c => c.Adapt<CourseDetailDto>() with
        {
            AccessType = c.AccessType.ToString()
        }).ToList();

        var paginatedList = new PaginatedList<CourseDetailDto>(items, totalCount, pageIndex, pageSize);
        return ApiResponse.Ok(paginatedList);
    }
}
