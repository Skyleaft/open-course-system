using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Features.GetQuestion;

public sealed class GetQuestionQueryHandler : IQueryHandler<GetQuestionQuery, ApiResponse<QuestionResultDto>>
{
    private readonly ExamsDbContext _dbContext;

    public GetQuestionQueryHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<QuestionResultDto>> Handle(GetQuestionQuery query, CancellationToken cancellationToken)
    {
        var question = await _dbContext.BankQuestions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == query.QuestionId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("Question not found in question bank.");
        }

        var bank = await _dbContext.QuestionBanks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == question.BankId, cancellationToken);

        var result = new QuestionResultDto(
            question.Id,
            null,
            null,
            question.QuestionText,
            question.Type.ToString(),
            question.Points,
            question.OrderIndex,
            question.Explanation,
            bank?.Category,
            bank?.Tags ?? [],
            question.Options.Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect)).ToList(),
            bank?.CreatedBy ?? Guid.Empty,
            bank?.UpdatedBy,
            bank?.CreatedAtUtc ?? DateTime.UtcNow,
            question.BankId
        );

        return ApiResponse.Ok(result);
    }
}
