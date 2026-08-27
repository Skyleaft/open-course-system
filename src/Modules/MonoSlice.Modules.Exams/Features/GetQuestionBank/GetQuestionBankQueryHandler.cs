using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetQuestionBank;

public sealed class GetQuestionBankQueryHandler : IQueryHandler<GetQuestionBankQuery, ApiResponse<QuestionBankDetailDto>>
{
    private readonly ExamsDbContext _dbContext;

    public GetQuestionBankQueryHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<QuestionBankDetailDto>> Handle(GetQuestionBankQuery query, CancellationToken cancellationToken)
    {
        var bank = await _dbContext.QuestionBanks
            .AsNoTracking()
            .Include(b => b.Questions)
            .FirstOrDefaultAsync(b => b.Id == query.Id, cancellationToken);

        if (bank is null)
        {
            return ApiResponse.Fail<QuestionBankDetailDto>("Question Bank pool not found.", 404);
        }

        var dto = new QuestionBankDetailDto(
            bank.Id,
            bank.Title,
            bank.Description,
            bank.Category,
            bank.Tags,
            bank.CreatedBy,
            bank.UpdatedBy,
            bank.CreatedAtUtc,
            bank.UpdatedAtUtc,
            bank.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new BankQuestionDto(
                    q.Id,
                    q.BankId,
                    q.QuestionText,
                    q.Type.ToString(),
                    q.Points,
                    q.OrderIndex,
                    q.Explanation,
                    q.Options,
                    q.GradingMethod.ToString()
                ))
                .ToList()
        );

        return ApiResponse.Ok(dto);
    }
}
