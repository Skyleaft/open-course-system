using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.ListExams;

public sealed class ListExamsQueryHandler : IQueryHandler<ListExamsQuery, ApiResponse<PaginatedList<ExamSummaryDto>>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListExamsQueryHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<PaginatedList<ExamSummaryDto>>> Handle(ListExamsQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Exams.AsNoTracking();

        // If user is not Admin/Instructor, they only see published exams
        var isElevated = _currentUser.IsAuthenticated && (_currentUser.IsInRole("Admin") || _currentUser.IsInRole("Instructor"));

        if (!isElevated)
        {
            dbQuery = dbQuery.Where(e => e.IsPublished);
        }
        else if (query.IsPublished.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.IsPublished == query.IsPublished.Value);
        }

        if (query.CourseId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.CourseId == query.CourseId.Value);
        }

        if (query.Mode.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Mode == query.Mode.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(e => e.Title.ToLower().Contains(search) || (e.Description != null && e.Description.ToLower().Contains(search)));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await dbQuery
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExamSummaryDto(
                e.Id,
                e.CourseId,
                e.InstructorId,
                e.Title,
                e.Description,
                e.Mode.ToString(),
                e.DurationMinutes,
                e.PassingScore,
                e.MaxAllowedViolations,
                e.MaxAttempts,
                e.AvailableFromUtc,
                e.AvailableToUtc,
                e.IsPublished,
                e.Questions.Count,
                e.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<ExamSummaryDto>(items, totalCount, pageIndex, pageSize);
        return ApiResponse.Ok(paginated);
    }
}
