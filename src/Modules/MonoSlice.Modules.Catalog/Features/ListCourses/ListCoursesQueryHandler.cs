using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.ListCourses;

public sealed class ListCoursesQueryHandler : IQueryHandler<ListCoursesQuery, ApiResponse<PaginatedList<CourseDetailDto>>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListCoursesQueryHandler(CoursesDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<PaginatedList<CourseDetailDto>>> Handle(
        ListCoursesQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Courses
            .AsNoTracking();

        // Role-based visibility: only Admin and Instructor can filter or view unpublished courses
        var isPrivileged = _currentUser.IsAuthenticated && (
            _currentUser.IsInRole("Admin") ||
            _currentUser.IsInRole("Instructor") ||
            _currentUser.Roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(r, "Instructor", StringComparison.OrdinalIgnoreCase))
        );

        if (isPrivileged)
        {
            if (query.IsPublished.HasValue)
            {
                dbQuery = dbQuery.Where(c => c.IsPublished == query.IsPublished.Value);
            }
        }
        else
        {
            // Anonymous and standard users only see published courses
            dbQuery = dbQuery.Where(c => c.IsPublished);
        }

        // Search term filter (Title or Description)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(c => c.Title.ToLower().Contains(search) ||
                                         (c.Description != null && c.Description.ToLower().Contains(search)));
        }

        // Course access type filter
        if (!string.IsNullOrWhiteSpace(query.AccessType) && Enum.TryParse<CourseAccessType>(query.AccessType, true, out var accessType))
        {
            dbQuery = dbQuery.Where(c => c.AccessType == accessType);
        }

        // Instructor filter
        if (query.InstructorId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.InstructorId == query.InstructorId.Value);
        }

        // Price range filters
        if (query.MinPrice.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Price <= query.MaxPrice.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        // Sorting
        var isDescending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(query.SortOrder, "descending", StringComparison.OrdinalIgnoreCase);

        var sortBy = query.SortBy?.Trim().ToLowerInvariant();

        dbQuery = sortBy switch
        {
            "title" => isDescending ? dbQuery.OrderByDescending(c => c.Title) : dbQuery.OrderBy(c => c.Title),
            "price" => isDescending ? dbQuery.OrderByDescending(c => c.Price) : dbQuery.OrderBy(c => c.Price),
            "updatedat" => isDescending ? dbQuery.OrderByDescending(c => c.UpdatedAtUtc) : dbQuery.OrderBy(c => c.UpdatedAtUtc),
            "createdat" => isDescending || string.IsNullOrWhiteSpace(query.SortOrder)
                ? dbQuery.OrderByDescending(c => c.CreatedAtUtc)
                : dbQuery.OrderBy(c => c.CreatedAtUtc),
            _ => isDescending
                ? dbQuery.OrderByDescending(c => c.CreatedAtUtc)
                : (string.IsNullOrWhiteSpace(query.SortOrder) ? dbQuery.OrderByDescending(c => c.CreatedAtUtc) : dbQuery.OrderBy(c => c.CreatedAtUtc))
        };

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var courses = await dbQuery
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
