using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Modules.Exams.Features.DeleteExam;
using MonoSlice.Modules.Exams.Features.DeleteQuestion;
using MonoSlice.Modules.Exams.Features.GetQuestion;
using MonoSlice.Modules.Exams.Features.ListExams;
using MonoSlice.Modules.Exams.Features.UpdateQuestion;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class ExamManagementCommandHandlerTests
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;

    public ExamManagementCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExamsDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _cacheService = Substitute.For<ICacheService>();
        _eventBus = Substitute.For<IEventBus>();
    }

    [Fact]
    public async Task DeleteExam_ShouldDeleteExamAndCascades_AndPublishIntegrationEvent()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.UserId.Returns(instructorId);
        _currentUser.IsInRole("Instructor").Returns(true);

        var qb = QuestionBank.Create(instructorId, "Bank 1", "Exp");
        qb.AddQuestion("Q1", QuestionType.SingleChoice, 5m, "Exp", [new(Guid.CreateVersion7(), "A", true)]);
        _dbContext.QuestionBanks.Add(qb);

        var exam = QuizExam.Create(instructorId, "Exam to Delete", "Desc", 60, 75m);
        exam.AddSection(qb.Id, "Section 1");

        _dbContext.Exams.Add(exam);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteExamCommandHandler(
            _dbContext,
            _currentUser,
            _cacheService,
            _eventBus,
            NullLogger<DeleteExamCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteExamCommand(exam.Id), CancellationToken.None);

        Assert.True(result.Data);
        Assert.Null(await _dbContext.Exams.FindAsync(exam.Id));
        Assert.Empty(await _dbContext.Sections.Where(s => s.ExamId == exam.Id).ToListAsync());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ExamDeletedIntegrationEvent>(e => e.ExamId == exam.Id && e.InstructorId == instructorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteExam_ByUnauthorizedUser_ShouldThrowBusinessRuleException()
    {
        var instructorId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();

        _currentUser.UserId.Returns(otherUserId);
        _currentUser.IsInRole("Admin").Returns(false);
        _currentUser.IsInRole("Instructor").Returns(true);

        var exam = QuizExam.Create(instructorId, "Protected Exam", "Desc", 60, 75m);
        _dbContext.Exams.Add(exam);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteExamCommandHandler(
            _dbContext,
            _currentUser,
            _cacheService,
            _eventBus,
            NullLogger<DeleteExamCommandHandler>.Instance);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new DeleteExamCommand(exam.Id), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task QuestionBankCrud_GetUpdateAndDelete_ShouldSucceed()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);
        _currentUser.IsInRole("Instructor").Returns(true);

        var qb = QuestionBank.Create(
            instructorId,
            "Package 1",
            "Description",
            category: "General",
            tags: ["test"]);

        var q = qb.AddQuestion(
            "Original Question",
            QuestionType.SingleChoice,
            5m,
            "Explanation",
            [
                new(Guid.CreateVersion7(), "Option 1", true),
                new(Guid.CreateVersion7(), "Option 2", false)
            ]);

        _dbContext.QuestionBanks.Add(qb);
        await _dbContext.SaveChangesAsync();

        // 1. Get Question
        var getHandler = new GetQuestionQueryHandler(_dbContext);
        var getRes = await getHandler.Handle(new GetQuestionQuery(q.Id), CancellationToken.None);
        Assert.NotNull(getRes.Data);
        Assert.Equal("Original Question", getRes.Data.QuestionText);

        // 2. Update Question
        var updateHandler = new UpdateQuestionCommandHandler(_dbContext, _currentUser, _cacheService);
        var updateRes = await updateHandler.Handle(new UpdateQuestionCommand
        {
            QuestionId = q.Id,
            QuestionText = "Updated Question Title",
            Type = QuestionType.MultipleChoice,
            Points = 10m,
            Explanation = "Updated Exp",
            Category = "Advanced",
            Tags = ["updated"],
            Options =
            [
                new(null, "New Option 1", true),
                new(null, "New Option 2", true)
            ]
        }, CancellationToken.None);

        Assert.Equal("Updated Question Title", updateRes.Data.QuestionText);
        Assert.Equal(10m, updateRes.Data.Points);
        Assert.Equal("MultipleChoice", updateRes.Data.Type);
        Assert.Equal("Advanced", updateRes.Data.Category);

        // 3. Delete Question
        var deleteHandler = new DeleteQuestionCommandHandler(_dbContext, _currentUser);
        var deleteRes = await deleteHandler.Handle(new DeleteQuestionCommand(q.Id), CancellationToken.None);
        Assert.True(deleteRes.Data);

        var deletedQuestion = await _dbContext.BankQuestions.FindAsync(q.Id);
        Assert.Null(deletedQuestion);
    }

    [Fact]
    public async Task ListExams_ShouldFilterAndPaginateCorrectly()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Instructor").Returns(true);

        var instructorId = Guid.CreateVersion7();
        var exam1 = QuizExam.Create(instructorId, "Microservices Architect Exam", "Desc", 60, 75m);
        var exam2 = QuizExam.Create(instructorId, "Basic SQL Practice", "Desc", 30, 60m, ruleConfig: ExamRuleConfig.Practice());

        _dbContext.Exams.AddRange(exam1, exam2);
        await _dbContext.SaveChangesAsync();

        var handler = new ListExamsQueryHandler(_dbContext, _currentUser);
        var res = await handler.Handle(new ListExamsQuery(searchTerm: "Microservices"), CancellationToken.None);

        Assert.NotNull(res.Data);
        Assert.Single(res.Data.Items);
        Assert.Equal("Microservices Architect Exam", res.Data.Items[0].Title);
    }
}
