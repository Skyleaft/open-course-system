using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.SaveAnswer;
using MonoSlice.Modules.Exams.Features.SubmitExam;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Exams.Domain.Services;

public sealed class ExamFinalizerService : IExamFinalizerService
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly IEventStreamPublisher _eventPublisher;

    public ExamFinalizerService(
        ExamsDbContext dbContext,
        ICacheService cacheService,
        IEventStreamPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _eventPublisher = eventPublisher;
    }

    public async Task<ExamFinalResultDto> FinalizeAndGradeSubmissionAsync(
        Guid submissionId,
        SubmissionStatus targetStatus = SubmissionStatus.Completed,
        string? disqualificationReason = null,
        CancellationToken ct = default)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), submissionId);
        }

        if (submission.Status != SubmissionStatus.InProgress)
        {
            return new ExamFinalResultDto(
                submission.Id,
                submission.ExamId,
                submission.Status.ToString(),
                submission.Score ?? 0m,
                submission.IsPassed ?? false,
                submission.SubmittedAtUtc ?? DateTime.UtcNow,
                0,
                submission.Answers.Count);
        }

        var exam = await _dbContext.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, ct);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), submission.ExamId);
        }

        // 1. Flush any buffered answers from Redis into PostgreSQL entity
        var cachedAnswers = await _cacheService.GetAsync<Dictionary<Guid, CachedAnswerDto>>(
            $"exam_answers:{submission.Id}", ct);

        if (cachedAnswers is not null && cachedAnswers.Count > 0)
        {
            foreach (var cached in cachedAnswers.Values)
            {
                submission.SaveAnswer(cached.QuestionId, cached.SelectedOptionIds, cached.EssayText);
            }
        }

        // 2. Perform Automated Objective Grading
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
                    // Essay score remains pending manual grading
                    break;
            }
        }

        var calculatedPercentage = totalPossiblePoints > 0
            ? (earnedPoints / totalPossiblePoints) * 100m
            : 0m;

        // 3. Apply final status
        if (targetStatus == SubmissionStatus.Disqualified)
        {
            submission.Disqualify(disqualificationReason ?? "Disqualified by proctor/anti-cheat system.");
        }
        else
        {
            submission.Complete(calculatedPercentage, exam.PassingScore);
            if (targetStatus == SubmissionStatus.TimedOut)
            {
                submission.MarkTimedOut();
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        // 4. Clean up Redis cache buffers
        await _cacheService.RemoveAsync($"exam_session:{submission.Id}", ct);
        await _cacheService.RemoveAsync($"exam_answers:{submission.Id}", ct);
        await _cacheService.RemoveAsync($"exam_liveness:{submission.Id}", ct);

        // 5. Publish integration event
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
            ct: ct);

        return new ExamFinalResultDto(
            submission.Id,
            exam.Id,
            submission.Status.ToString(),
            submission.Score ?? 0m,
            submission.IsPassed ?? false,
            submission.SubmittedAtUtc ?? DateTime.UtcNow,
            exam.Questions.Count,
            submission.Answers.Count);
    }
}
