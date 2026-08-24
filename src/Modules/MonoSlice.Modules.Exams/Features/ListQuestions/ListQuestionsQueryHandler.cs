using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ListQuestions;

public sealed class ListQuestionsQueryHandler : IQueryHandler<ListQuestionsQuery, ApiResponse<PaginatedList<QuestionItemDto>>>
{
    private readonly ExamsDbContext _dbContext;

    public ListQuestionsQueryHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<PaginatedList<QuestionItemDto>>> Handle(ListQuestionsQuery query, CancellationToken cancellationToken)
    {
        var dbQuery = from q in _dbContext.BankQuestions.AsNoTracking()
                      join b in _dbContext.QuestionBanks.AsNoTracking() on q.BankId equals b.Id
                      select new
                      {
                          Question = q,
                          Bank = b
                      };

        if (query.BankId.HasValue && query.BankId.Value != Guid.Empty)
        {
            dbQuery = dbQuery.Where(x => x.Question.BankId == query.BankId.Value);
        }

        if (query.Type.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Question.Type == query.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var cat = query.Category.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Bank.Category != null && x.Bank.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(x => x.Question.QuestionText.ToLower().Contains(search) || (x.Question.Explanation != null && x.Question.Explanation.ToLower().Contains(search)));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await dbQuery
            .OrderBy(x => x.Question.OrderIndex)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QuestionItemDto(
                x.Question.Id,
                x.Question.BankId,
                x.Bank.Title,
                x.Bank.Category,
                x.Question.QuestionText,
                x.Question.Type.ToString(),
                x.Question.Points,
                x.Question.OrderIndex,
                x.Question.Explanation,
                x.Question.Options,
                x.Bank.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var paginated = new PaginatedList<QuestionItemDto>(items, totalCount, pageIndex, pageSize);
        return ApiResponse.Ok(paginated);
    }
}
