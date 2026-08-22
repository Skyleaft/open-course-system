using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Features.SaveAnswer;
using MonoSlice.Modules.Exams.Features.StartExam;
using MonoSlice.Modules.Exams.Features.SubmitExam;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class ExamExecutionCommandHandlerTests
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IEventStreamPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IExamFinalizerService _finalizerService;

    public ExamExecutionCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new ExamsDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _cacheService = Substitute.For<ICacheService>();
        _eventPublisher = Substitute.For<IEventStreamPublisher>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _finalizerService = new ExamFinalizerService(_dbContext, _cacheService, _eventPublisher);
    }

    [Fact]
    public async Task ExamExecution_FullFlow_ShouldCalculateScoreAndPublishEvent()
    {
        var instructorId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(studentId);

        // 1. Create and publish exam with 2 questions (Single Choice 10 pts, True False 10 pts)
        var exam = QuizExam.Create(instructorId, "Unit Test Quiz", "Desc", QuizMode.RealExam, 45, passingScore: 50m);
        
        var opt1Correct = Guid.CreateVersion7();
        var opt1Wrong = Guid.CreateVersion7();
        var q1 = exam.AddQuestion("Q1: 2 + 2 = ?", QuestionType.SingleChoice, 10m, "2+2=4", new List<QuestionOption>
        {
            new(opt1Correct, "4", true),
            new(opt1Wrong, "5", false)
        });

        var opt2True = Guid.CreateVersion7();
        var opt2False = Guid.CreateVersion7();
        var q2 = exam.AddQuestion("Q2: C# is compiled to IL?", QuestionType.TrueFalse, 10m, "True", new List<QuestionOption>
        {
            new(opt2True, "True", true),
            new(opt2False, "False", false)
        });

        exam.Publish();
        await _dbContext.Exams.AddAsync(exam);
        await _dbContext.SaveChangesAsync();

        // 2. Start Exam
        var startHandler = new StartExamCommandHandler(_dbContext, _currentUser, _cacheService, _serviceProvider);
        var startResult = await startHandler.Handle(new StartExamCommand(exam.Id), CancellationToken.None);

        Assert.True(startResult.Success);
        Assert.NotNull(startResult.Data);
        var submissionId = startResult.Data.SubmissionId;

        // 3. Save Answers (Q1 correct, Q2 wrong)
        var cachedAnswersDict = new Dictionary<Guid, MonoSlice.Modules.Exams.Features.SaveAnswer.CachedAnswerDto>();
        _cacheService.SetAsync(
            Arg.Is<string>(k => k == $"exam_answers:{submissionId}"),
            Arg.Do<Dictionary<Guid, MonoSlice.Modules.Exams.Features.SaveAnswer.CachedAnswerDto>>(d => cachedAnswersDict = d),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());

        _cacheService.GetAsync<Dictionary<Guid, MonoSlice.Modules.Exams.Features.SaveAnswer.CachedAnswerDto>>(
            Arg.Is<string>(k => k == $"exam_answers:{submissionId}"),
            Arg.Any<CancellationToken>())
            .Returns(_ => cachedAnswersDict);

        var saveHandler = new SaveAnswerCommandHandler(_cacheService, _currentUser);
        await saveHandler.Handle(new SaveAnswerCommand
        {
            SubmissionId = submissionId,
            QuestionId = q1.Id,
            SelectedOptionIds = [opt1Correct]
        }, CancellationToken.None);

        await saveHandler.Handle(new SaveAnswerCommand
        {
            SubmissionId = submissionId,
            QuestionId = q2.Id,
            SelectedOptionIds = [opt2False]
        }, CancellationToken.None);

        // 4. Submit Exam
        var submitHandler = new SubmitExamCommandHandler(_finalizerService, _currentUser);
        var submitResult = await submitHandler.Handle(new SubmitExamCommand(submissionId), CancellationToken.None);

        Assert.True(submitResult.Success);
        Assert.NotNull(submitResult.Data);
        Assert.Equal(50m, submitResult.Data.Score); // 10 out of 20 points = 50%
        Assert.True(submitResult.Data.IsPassed);   // passingScore = 50m

        // Verify event published
        await _eventPublisher.Received(1).PublishAsync(
            "stream:exam-events",
            Arg.Is<ExamSubmittedIntegrationEvent>(e => e.SubmissionId == submissionId && e.Score == 50m && e.IsPassed),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }
}
