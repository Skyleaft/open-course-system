using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.UpdateQuestion;

public sealed class UpdateQuestionCommandHandler : ICommandHandler<UpdateQuestionCommand, ApiResponse<QuestionResultDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;

    public UpdateQuestionCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<QuestionResultDto>> Handle(UpdateQuestionCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to update questions.");
        }

        var question = await _dbContext.BankQuestions
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("Question not found in question bank.");
        }

        var bank = await _dbContext.QuestionBanks
            .FirstOrDefaultAsync(b => b.Id == question.BankId, cancellationToken);

        if (bank is null)
        {
            throw new NotFoundException("Parent Question Bank not found.");
        }

        if (!_currentUser.IsInRole("Admin") && _currentUser.UserId != bank.CreatedBy)
        {
            throw new BusinessRuleException("You do not have permission to modify this question in the bank.");
        }

        var domainOptions = command.Options.Select(o => new QuestionOption(
            o.Id ?? Guid.CreateVersion7(),
            o.Text,
            o.IsCorrect
        )).ToList();

        question.Update(
            command.QuestionText,
            command.Type,
            command.Points,
            command.Explanation,
            domainOptions);

        bank.Update(_currentUser.UserId.Value, bank.Title, bank.Description, command.Category ?? bank.Category, command.Tags.Count > 0 ? command.Tags : bank.Tags);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new QuestionResultDto(
            question.Id,
            null,
            null,
            question.QuestionText,
            question.Type.ToString(),
            question.Points,
            question.OrderIndex,
            question.Explanation,
            bank.Category,
            bank.Tags,
            question.Options.Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect)).ToList(),
            bank.CreatedBy,
            bank.UpdatedBy,
            bank.CreatedAtUtc,
            bank.Id
        );

        return ApiResponse.Ok(result, "Question updated successfully.");
    }
}
