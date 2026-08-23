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
        var question = await _dbContext.Questions
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException("Quiz question not found.");
        }

        var exam = await _dbContext.Exams
            .FirstOrDefaultAsync(e => e.Id == question.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException("Parent examination not found.");
        }

        if (!_currentUser.IsInRole("Admin") && _currentUser.UserId != exam.InstructorId)
        {
            throw new BusinessRuleException("You do not have permission to modify questions in this exam.");
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate caches
        await _cacheService.RemoveAsync($"exam:{exam.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"exam:questions:{exam.Id}", cancellationToken);

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

        return ApiResponse.Ok(result, "Question updated successfully.");
    }
}
