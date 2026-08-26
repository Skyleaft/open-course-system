using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Features.ExamRules;
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
        var dbQuery = _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
            .AsQueryable();

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

        if (query.ExamRuleId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.ExamRuleId == query.ExamRuleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(e => e.Title.ToLower().Contains(search) || (e.Description != null && e.Description.ToLower().Contains(search)));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var rawList = await dbQuery
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawList.Select(e =>
        {
            var ruleConfigDto = new ExamRuleConfigDto(
                e.RuleConfig.Name,
                e.RuleConfig.CanTabSwitch,
                e.RuleConfig.MaxTabSwitchesAllowed,
                e.RuleConfig.RestrictClipboardAndMouse,
                e.RuleConfig.ForceFullscreen,
                e.RuleConfig.KeyboardDetection,
                e.RuleConfig.RequireCamera,
                e.RuleConfig.SnapshotIntervalSeconds,
                e.RuleConfig.RequireMicrophone,
                e.RuleConfig.MaxAllowedViolations,
                e.RuleConfig.AutoDisqualifyOnExceed);

            return new ExamSummaryDto(
                e.Id,
                e.InstructorId,
                e.Title,
                e.Description,
                e.ExamRuleId,
                ruleConfigDto,
                e.DurationMinutes,
                e.PassingScore,
                e.MaxAttempts,
                e.AvailableFromUtc,
                e.AvailableToUtc,
                e.IsPublished,
                e.Sections.Count,
                e.Sections.SelectMany(s => s.QuestionBank?.Questions ?? Enumerable.Empty<Domain.BankQuestion>()).Count(),
                e.CreatedBy,
                e.UpdatedBy,
                e.CreatedAtUtc
            );
        }).ToList();

        var paginated = new PaginatedList<ExamSummaryDto>(items, totalCount, pageIndex, pageSize);
        return ApiResponse.Ok(paginated);
    }
}
