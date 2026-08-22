using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Exams.Features.SubmitExam;

public sealed class SubmitExamCommandHandler : ICommandHandler<SubmitExamCommand, ApiResponse<ExamFinalResultDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IEventStreamPublisher _eventPublisher;

    public SubmitExamCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        ICacheService cacheService,
        IEventStreamPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cacheService = cacheService;
        _eventPublisher = eventPublisher;
    }

    public async ValueTask<ApiResponse<ExamFinalResultDto>> Handle(
        SubmitExamCommand command,
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

        if (submission.Status != SubmissionStatus.InProgress)
        {
            throw new BusinessRuleException($"Exam attempt is already {submission.Status}.");
        }

        var exam = await _dbContext.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), submission.ExamId);
        }

        // Automatic Objective Grading
        decimal totalPossiblePoints = 0m;
        decimal earnedPoints = 0m;

        foreach (var question in exam.Questions)
        {
            totalPossiblePoints += question.Points;

            var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (answer is null)
            {
                continue;
            }

            var correctOptionIds = question.Options
                .Where(o => o.IsCorrect)
                .Select(o => o.Id)
                .ToHashSet();

            switch (question.Type)
            {
                case QuestionType.SingleChoice:
                case QuestionType.TrueFalse:
                    if (answer.SelectedOptionIds.Count == 1 && correctOptionIds.Contains(answer.SelectedOptionIds[0]))
                    {
                        answer.SetAwardedScore(question.Points);
                        earnedPoints += question.Points;
                    }
                    else
                    {
                        answer.SetAwardedScore(0m);
                    }
                    break;

                case QuestionType.MultipleChoice:
                    var selected = answer.SelectedOptionIds.ToHashSet();
                    if (selected.SetEquals(correctOptionIds))
                    {
                        answer.SetAwardedScore(question.Points);
                        earnedPoints += question.Points;
                    }
                    else
                    {
                        answer.SetAwardedScore(0m);
                    }
                    break;

                case QuestionType.Essay:
                    // Score pending manual evaluation
                    break;
            }
        }

        var calculatedPercentage = totalPossiblePoints > 0
            ? (earnedPoints / totalPossiblePoints) * 100m
            : 0m;

        submission.Complete(calculatedPercentage, exam.PassingScore);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Remove active exam session from Redis
        await _cacheService.RemoveAsync($"exam_session:{submission.Id}", cancellationToken);

        // Publish integration event to Redis stream
        var integrationEvent = new ExamSubmittedIntegrationEvent(
            submission.Id,
            exam.Id,
            submission.StudentId,
            submission.Score ?? 0m,
            submission.IsPassed ?? false,
            submission.SubmittedAtUtc ?? DateTime.UtcNow);

        await _eventPublisher.PublishAsync(
            "stream:exam-events",
            integrationEvent,
            ct: cancellationToken);

        var resultDto = new ExamFinalResultDto(
            submission.Id,
            exam.Id,
            submission.Status.ToString(),
            submission.Score ?? 0m,
            submission.IsPassed ?? false,
            submission.SubmittedAtUtc ?? DateTime.UtcNow,
            exam.Questions.Count,
            submission.Answers.Count);

        return ApiResponse.Ok(resultDto, "Exam submitted and graded successfully.");
    }
}
