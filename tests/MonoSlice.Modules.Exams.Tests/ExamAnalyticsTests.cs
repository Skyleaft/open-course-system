using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.Analytics.GetExamAnalytics;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public sealed class ExamAnalyticsTests
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly GetExamAnalyticsQueryHandler _handler;

    public ExamAnalyticsTests()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExamsAnalyticsTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ExamsDbContext(options);
        _cacheService = Substitute.For<ICacheService>();

        // Configure cache to execute factory directly
        _cacheService.GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<ExamAnalyticsDto>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task<ExamAnalyticsDto>>>()());

        _handler = new GetExamAnalyticsQueryHandler(_dbContext, _cacheService);
    }

    [Fact]
    public async Task Handle_ShouldCalculate_AccuratePsychometricsAndScoreDistribution()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var bank = QuestionBank.Create(instructorId, "Psychometrics Pool", "Testing psychometric item analysis");
        var q1 = bank.AddQuestion("Easy Question 1", QuestionType.SingleChoice, 10m, "Explanation 1", []);
        var q2 = bank.AddQuestion("Hard Question 2", QuestionType.SingleChoice, 10m, "Explanation 2", []);
        _dbContext.QuestionBanks.Add(bank);

        var exam = QuizExam.Create(
            instructorId,
            "Final Assessment",
            "Comprehensive Assessment",
            durationMinutes: 60,
            passingScore: 70m,
            ruleConfig: ExamRuleConfig.Practice());

        exam.AddSection(bank.Id, "Core Section", pointsOverride: 10m);
        _dbContext.Exams.Add(exam);

        // Add 10 submissions: 7 high scorers, 3 low scorers
        for (int i = 0; i < 10; i++)
        {
            var studentId = Guid.NewGuid();
            var submission = QuizSubmission.Create(
                exam.Id,
                studentId,
                60,
                12345,
                Guid.NewGuid().ToString(),
                null,
                1);

            decimal score = i < 7 ? 80m + (i * 2) : 30m + (i * 5);
            submission.Complete(score, exam.PassingScore);
            _dbContext.Submissions.Add(submission);

            // Add answers: q1 answered correctly by all, q2 answered correctly only by top scorers
            var ans1 = StudentAnswer.Create(submission.Id, q1.Id, [], null);
            ans1.SetAwardedScore(10m);
            _dbContext.StudentAnswers.Add(ans1);

            var ans2 = StudentAnswer.Create(submission.Id, q2.Id, [], null);
            if (i < 3)
            {
                ans2.SetAwardedScore(10m);
            }
            else
            {
                ans2.SetAwardedScore(0m);
            }
            _dbContext.StudentAnswers.Add(ans2);
        }

        await _dbContext.SaveChangesAsync();

        // Act
        var query = new GetExamAnalyticsQuery { ExamId = exam.Id };
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(10, result.Data!.TotalSubmissions);
        Assert.Equal(10, result.Data.CompletedSubmissions);
        Assert.Equal(7, result.Data.PassedCount);
        Assert.Equal(3, result.Data.FailedCount);
        Assert.Equal(70.0, result.Data.PassRate);

        // Verify Question 1 (Easy, 100% correct)
        var q1Stats = result.Data.ItemPsychometrics.FirstOrDefault(x => x.QuestionId == q1.Id);
        Assert.NotNull(q1Stats);
        Assert.Equal(1.0, q1Stats!.DifficultyIndex);
        Assert.Equal("Easy", q1Stats.DifficultyLabel);

        // Verify Question 2 (Medium/Hard, 30% correct)
        var q2Stats = result.Data.ItemPsychometrics.FirstOrDefault(x => x.QuestionId == q2.Id);
        Assert.NotNull(q2Stats);
        Assert.Equal(0.3, q2Stats!.DifficultyIndex);
        Assert.Equal("Medium", q2Stats.DifficultyLabel);
    }
}
