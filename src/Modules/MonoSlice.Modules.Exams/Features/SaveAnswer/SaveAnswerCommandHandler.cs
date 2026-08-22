using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.SaveAnswer;

public sealed class SaveAnswerCommandHandler : ICommandHandler<SaveAnswerCommand, ApiResponse>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SaveAnswerCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse> Handle(
        SaveAnswerCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var submission = await _dbContext.Submissions
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), command.SubmissionId);
        }

        if (submission.StudentId != _currentUser.UserId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this exam attempt.");
        }

        submission.SaveAnswer(command.QuestionId, command.SelectedOptionIds, command.EssayText);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Answer saved successfully.");
    }
}
