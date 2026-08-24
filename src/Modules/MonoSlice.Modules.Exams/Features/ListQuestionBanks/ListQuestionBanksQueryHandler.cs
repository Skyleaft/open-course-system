using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ListQuestionBanks;

public sealed class ListQuestionBanksQueryHandler : IQueryHandler<ListQuestionBanksQuery, ApiResponse<PaginatedList<QuestionBankSummaryDto>>>
{
    private readonly ExamsDbContext _dbContext;

    public ListQuestionBanksQueryHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<PaginatedList<QuestionBankSummaryDto>>> Handle(ListQuestionBanksQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.QuestionBanks.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var cat = query.Category.Trim().ToLower();
            dbQuery = dbQuery.Where(b => b.Category != null && b.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(b => b.Title.ToLower().Contains(search) || (b.Description != null && b.Description.ToLower().Contains(search)));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await dbQuery
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new QuestionBankSummaryDto(
                b.Id,
                b.Title,
                b.Description,
                b.Category,
                b.Tags,
                b.Questions.Count,
                b.CreatedBy,
                b.UpdatedBy,
                b.CreatedAtUtc,
                b.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<QuestionBankSummaryDto>(items, totalCount, pageIndex, pageSize);
        return ApiResponse.Ok(paginated);
    }
}
