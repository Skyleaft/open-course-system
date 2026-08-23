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
    private readonly ICacheService _cacheService;

    public DeleteQuestionCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<bool>> Handle(DeleteQuestionCommand command, CancellationToken cancellationToken)
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
            throw new BusinessRuleException("You do not have permission to delete questions from this exam.");
        }

        _dbContext.Questions.Remove(question);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate caches
        await _cacheService.RemoveAsync($"exam:{exam.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"exam:questions:{exam.Id}", cancellationToken);

        return ApiResponse.Ok(true, "Question deleted successfully.");
    }
}
