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
            return ApiResponse.Fail<QuestionResultDto>("Authentication required to update questions.", 401);
        }

        var question = await _dbContext.BankQuestions
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId, cancellationToken);

        if (question is null)
        {
            return ApiResponse.Fail<QuestionResultDto>("Question not found in question bank.", 404);
        }

        var bank = await _dbContext.QuestionBanks
            .FirstOrDefaultAsync(b => b.Id == question.BankId, cancellationToken);

        if (bank is null)
        {
            return ApiResponse.Fail<QuestionResultDto>("Parent Question Bank not found.", 404);
        }

        if (!_currentUser.IsInRole("Admin") && !_currentUser.IsInRole("Instructor") && _currentUser.UserId != bank.CreatedBy)
        {
            return ApiResponse.Fail<QuestionResultDto>("You do not have permission to modify this question in the bank.", 403);
        }

        var domainOptions = (command.Options ?? []).Select(o => new QuestionOption(
            o.Id.HasValue && o.Id.Value != Guid.Empty ? o.Id.Value : Guid.CreateVersion7(),
            o.Text ?? string.Empty,
            o.IsCorrect
        )).ToList();

        question.Update(
            command.QuestionText,
            command.Type,
            command.Points > 0 ? command.Points : 1m,
            command.Explanation,
            domainOptions);

        if (!string.IsNullOrWhiteSpace(command.Category) || (command.Tags != null && command.Tags.Count > 0))
        {
            bank.Update(
                _currentUser.UserId.Value,
                bank.Title,
                bank.Description,
                command.Category ?? bank.Category,
                command.Tags != null && command.Tags.Count > 0 ? command.Tags : bank.Tags);
        }

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
