using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.Analytics.GetExamAnalytics;

public sealed class GetExamAnalyticsQueryHandler : IQueryHandler<GetExamAnalyticsQuery, ApiResponse<ExamAnalyticsDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetExamAnalyticsQueryHandler(
        ExamsDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<ExamAnalyticsDto>> Handle(GetExamAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Sections)
            .FirstOrDefaultAsync(e => e.Id == query.ExamId, cancellationToken);

        if (exam is null)
        {
            return ApiResponse.Fail<ExamAnalyticsDto>("Exam not found.", 404);
        }

        var cacheKey = $"cache:dashboard:instructor:exams:{query.ExamId}:analytics";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var submissions = await _dbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ExamId == query.ExamId)
                .ToListAsync(cancellationToken);

            var totalSubmissions = submissions.Count;
            var completedSubmissions = submissions.Where(s => s.Status == SubmissionStatus.Completed).ToList();
            var disqualifiedCount = submissions.Count(s => s.Status == SubmissionStatus.Disqualified);

            var scores = completedSubmissions
                .Select(s => (double)(s.Score ?? 0m))
                .OrderBy(s => s)
                .ToList();

            decimal averageScore = 0m;
            decimal medianScore = 0m;
            decimal minScore = 0m;
            decimal maxScore = 0m;
            decimal stdDev = 0m;
            int passedCount = 0;
            int failedCount = 0;
            double passRate = 0.0;

            if (scores.Count > 0)
            {
                var avg = scores.Average();
                averageScore = Math.Round((decimal)avg, 2);
                minScore = (decimal)scores.First();
                maxScore = (decimal)scores.Last();

                int mid = scores.Count / 2;
                medianScore = (decimal)(scores.Count % 2 != 0 ? scores[mid] : (scores[mid - 1] + scores[mid]) / 2.0);

                var variance = scores.Select(s => Math.Pow(s - avg, 2)).Average();
                stdDev = Math.Round((decimal)Math.Sqrt(variance), 2);

                passedCount = completedSubmissions.Count(s => (s.Score ?? 0m) >= exam.PassingScore);
                failedCount = completedSubmissions.Count - passedCount;
                passRate = Math.Round((double)passedCount / completedSubmissions.Count * 100, 2);
            }

            // Score distribution buckets
            var buckets = new List<ScoreDistributionBucketDto>
            {
                new() { RangeLabel = "0 - 20", MinScore = 0, MaxScore = 20, StudentCount = completedSubmissions.Count(s => (s.Score ?? 0m) >= 0 && (s.Score ?? 0m) <= 20) },
                new() { RangeLabel = "21 - 40", MinScore = 21, MaxScore = 40, StudentCount = completedSubmissions.Count(s => (s.Score ?? 0m) > 20 && (s.Score ?? 0m) <= 40) },
                new() { RangeLabel = "41 - 60", MinScore = 41, MaxScore = 60, StudentCount = completedSubmissions.Count(s => (s.Score ?? 0m) > 40 && (s.Score ?? 0m) <= 60) },
                new() { RangeLabel = "61 - 80", MinScore = 61, MaxScore = 80, StudentCount = completedSubmissions.Count(s => (s.Score ?? 0m) > 60 && (s.Score ?? 0m) <= 80) },
                new() { RangeLabel = "81 - 100", MinScore = 81, MaxScore = 100, StudentCount = completedSubmissions.Count(s => (s.Score ?? 0m) > 80 && (s.Score ?? 0m) <= 100) }
            };

            // Psychometric item analysis
            var questionBankIds = exam.Sections.Select(s => s.QuestionBankId).Distinct().ToList();
            var questions = await _dbContext.BankQuestions
                .AsNoTracking()
                .Where(q => questionBankIds.Contains(q.BankId))
                .ToListAsync(cancellationToken);

            var submissionIds = completedSubmissions.Select(s => s.Id).ToList();
            var studentAnswers = await _dbContext.StudentAnswers
                .AsNoTracking()
                .Where(a => submissionIds.Contains(a.SubmissionId))
                .ToListAsync(cancellationToken);

            // Group submissions by score descending for 27% upper/lower group analysis
            var sortedSubmissions = completedSubmissions.OrderByDescending(s => s.Score ?? 0m).ToList();
            int nGroup = Math.Max(1, (int)Math.Round(sortedSubmissions.Count * 0.27));
            var upperSubmissions = sortedSubmissions.Take(nGroup).Select(s => s.Id).ToHashSet();
            var lowerSubmissions = sortedSubmissions.TakeLast(nGroup).Select(s => s.Id).ToHashSet();

            var itemPsychometrics = new List<ItemPsychometricDto>();

            foreach (var q in questions)
            {
                var answersForQuestion = studentAnswers.Where(a => a.QuestionId == q.Id).ToList();
                int totalAttempts = answersForQuestion.Count;
                int correctCount = answersForQuestion.Count(a => a.AwardedScore.HasValue && a.AwardedScore.Value > 0);

                double difficultyIndex = totalAttempts > 0 ? Math.Round((double)correctCount / totalAttempts, 2) : 0.5;
                string diffLabel = difficultyIndex < 0.30 ? "Hard" : difficultyIndex <= 0.70 ? "Medium" : "Easy";

                // Discrimination index (Upper 27% vs Lower 27%)
                double discriminationIndex = 0.0;
                string discStatus = "NeedsReview";

                if (sortedSubmissions.Count >= 4)
                {
                    int uCorrect = answersForQuestion.Count(a => upperSubmissions.Contains(a.SubmissionId) && a.AwardedScore.HasValue && a.AwardedScore.Value > 0);
                    int lCorrect = answersForQuestion.Count(a => lowerSubmissions.Contains(a.SubmissionId) && a.AwardedScore.HasValue && a.AwardedScore.Value > 0);
                    discriminationIndex = Math.Round((double)(uCorrect - lCorrect) / nGroup, 2);

                    discStatus = discriminationIndex >= 0.40 ? "Excellent" : discriminationIndex >= 0.20 ? "Good" : "NeedsReview";
                }
                else
                {
                    discStatus = "Good"; // Not enough data for 27% grouping
                }

                itemPsychometrics.Add(new ItemPsychometricDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.Type.ToString(),
                    MaxPoints = q.Points,
                    TotalAttempts = totalAttempts,
                    CorrectCount = correctCount,
                    DifficultyIndex = difficultyIndex,
                    DifficultyLabel = diffLabel,
                    DiscriminationIndex = discriminationIndex,
                    DiscriminationStatus = discStatus
                });
            }

            return new ExamAnalyticsDto
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                TotalSubmissions = totalSubmissions,
                CompletedSubmissions = completedSubmissions.Count,
                DisqualifiedSubmissions = disqualifiedCount,
                AverageScore = averageScore,
                MedianScore = medianScore,
                HighestScore = maxScore,
                LowestScore = minScore,
                StandardDeviation = stdDev,
                PassingScore = exam.PassingScore,
                PassedCount = passedCount,
                FailedCount = failedCount,
                PassRate = passRate,
                ScoreBuckets = buckets,
                ItemPsychometrics = itemPsychometrics
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
