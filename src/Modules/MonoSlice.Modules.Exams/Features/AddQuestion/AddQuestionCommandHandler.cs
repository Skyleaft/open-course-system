using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public sealed class AddQuestionCommandHandler : ICommandHandler<AddQuestionCommand, ApiResponse<QuestionResultDto>>
{
    private readonly ExamsDbContext _dbContext;

    public AddQuestionCommandHandler(ExamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<QuestionResultDto>> Handle(
        AddQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), command.ExamId);
        }

        var options = command.Options.Select(o => new QuestionOption(
            o.Id ?? Guid.CreateVersion7(),
            o.Text,
            o.IsCorrect
        )).ToList();

        var question = exam.AddQuestion(
            command.QuestionText,
            command.Type,
            command.Points,
            command.Explanation,
            options);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new QuestionResultDto(
            question.Id,
            question.ExamId,
            question.QuestionText,
            question.Type.ToString(),
            question.Points,
            question.OrderIndex,
            question.Explanation,
            question.Options.Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect)).ToList());

        return ApiResponse.Ok(result, "Question added successfully.");
    }
}
