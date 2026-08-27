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
            .Include(e => e.Sections)
                .ThenInclude(s => s.QuestionBank)
                .ThenInclude(qb => qb!.Questions)
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
        var totalQuestionsCount = 0;

        foreach (var section in exam.Sections.OrderBy(s => s.OrderIndex))
        {
            if (section.QuestionBank is null)
            {
                continue;
            }

            var questions = section.QuestionBank.Questions.OrderBy(q => q.OrderIndex).ToList();
            if (section.QuestionCount.HasValue && section.QuestionCount.Value > 0)
            {
                questions = questions.Take(section.QuestionCount.Value).ToList();
            }

            totalQuestionsCount += questions.Count;

            foreach (var question in questions)
            {
                var points = section.PointsOverride ?? question.Points;
                totalPossiblePoints += points;

                var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (answer is null)
                {
                    continue;
                }

                var correctOptionIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .ToHashSet();

                decimal questionAwardedScore = 0m;

                switch (question.Type)
                {
                    case QuestionType.SingleChoice:
                    case QuestionType.TrueFalse:
                        if (answer.SelectedOptionIds.Count == 1)
                        {
                            var selectedId = answer.SelectedOptionIds[0];
                            if (question.GradingMethod == GradingMethod.OptionWeighted)
                            {
                                var chosenOpt = question.Options.FirstOrDefault(o => o.Id == selectedId);
                                if (chosenOpt is not null)
                                {
                                    questionAwardedScore = Math.Max(0m, Math.Min(points, chosenOpt.Points));
                                }
                            }
                            else if (correctOptionIds.Contains(selectedId))
                            {
                                questionAwardedScore = points;
                            }
                        }
                        break;

                    case QuestionType.MultipleChoice:
                        var selectedOptionIds = answer.SelectedOptionIds.ToHashSet();
                        var correctOptions = question.Options.Where(o => o.IsCorrect).ToList();
                        var incorrectOptions = question.Options.Where(o => !o.IsCorrect).ToList();

                        var correctSelectedCount = selectedOptionIds.Count(id => correctOptions.Any(c => c.Id == id));
                        var incorrectSelectedCount = selectedOptionIds.Count(id => incorrectOptions.Any(i => i.Id == id));

                        switch (question.GradingMethod)
                        {
                            case GradingMethod.AllOrNothing:
                                if (correctOptions.Count > 0 &&
                                    correctSelectedCount == correctOptions.Count &&
                                    incorrectSelectedCount == 0)
                                {
                                    questionAwardedScore = points;
                                }
                                break;

                            case GradingMethod.PartialWithPenalty:
                                if (correctOptions.Count > 0)
                                {
                                    decimal pointPerCorrect = points / correctOptions.Count;
                                    decimal penaltyPerIncorrect = incorrectOptions.Count > 0
                                        ? points / incorrectOptions.Count
                                        : pointPerCorrect;

                                    decimal earned = (correctSelectedCount * pointPerCorrect) - (incorrectSelectedCount * penaltyPerIncorrect);
                                    questionAwardedScore = Math.Max(0m, Math.Min(points, Math.Round(earned, 2)));
                                }
                                break;

                            case GradingMethod.PartialWithoutPenalty:
                                if (correctOptions.Count > 0 && incorrectSelectedCount == 0)
                                {
                                    decimal pointPerCorrect = points / correctOptions.Count;
                                    decimal earned = correctSelectedCount * pointPerCorrect;
                                    questionAwardedScore = Math.Max(0m, Math.Min(points, Math.Round(earned, 2)));
                                }
                                break;

                            case GradingMethod.OptionWeighted:
                                decimal totalWeighted = 0m;
                                foreach (var optId in selectedOptionIds)
                                {
                                    var opt = question.Options.FirstOrDefault(o => o.Id == optId);
                                    if (opt is not null)
                                    {
                                        totalWeighted += opt.Points;
                                        if (opt.PenaltyPoints > 0m)
                                        {
                                            totalWeighted -= opt.PenaltyPoints;
                                        }
                                    }
                                }
                                questionAwardedScore = Math.Max(0m, Math.Min(points, Math.Round(totalWeighted, 2)));
                                break;
                        }
                        break;

                    case QuestionType.Essay:
                        // Essay score remains pending manual grading
                        break;
                }

                answer.SetAwardedScore(questionAwardedScore);
                earnedPoints += questionAwardedScore;
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
            totalQuestionsCount,
            submission.Answers.Count);
    }
}
