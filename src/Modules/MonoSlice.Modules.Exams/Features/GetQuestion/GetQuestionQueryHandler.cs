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
        var question = await _dbContext.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == query.QuestionId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("Quiz question not found.");
        }

        var result = new QuestionResultDto(
            question.Id,
            question.ExamId,
            question.QuestionText,
            question.Type.ToString(),
            question.Points,
            question.OrderIndex,
            question.Explanation,
            question.Options.Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect)).ToList()
        );

        return ApiResponse.Ok(result);
    }
}
