using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.DeleteQuestion;

public sealed class DeleteQuestionCommandHandler : ICommandHandler<DeleteQuestionCommand, ApiResponse<bool>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteQuestionCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<bool>> Handle(DeleteQuestionCommand command, CancellationToken cancellationToken)
    {
        var question = await _dbContext.BankQuestions
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("Question not found in question bank.");
        }

        var bank = await _dbContext.QuestionBanks
            .FirstOrDefaultAsync(b => b.Id == question.BankId, cancellationToken);

        if (bank is not null && !_currentUser.IsInRole("Admin") && _currentUser.UserId != bank.CreatedBy)
        {
            throw new BusinessRuleException("You do not have permission to delete this question from the bank.");
        }

        _dbContext.BankQuestions.Remove(question);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok(true, "Question deleted from bank successfully.");
    }
}
