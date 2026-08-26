using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Features.Proctor.ForceDisconnectCandidate;
using MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;
using MonoSlice.Modules.Exams.Features.Proctor.WarnCandidate;
using MonoSlice.Modules.Exams.Hubs;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class ProctorControlCommandHandlerTests
{
    private readonly ExamsDbContext _dbContext;
    private readonly IHubContext<ExamHub> _hubContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;
    private readonly IEventStreamPublisher _eventPublisher;
    private readonly IExamFinalizerService _finalizerService;

    public ProctorControlCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new ExamsDbContext(options);
        _hubContext = Substitute.For<IHubContext<ExamHub>>();
        _cacheService = Substitute.For<ICacheService>();
        _currentUser = Substitute.For<ICurrentUser>();
        _eventPublisher = Substitute.For<IEventStreamPublisher>();
        _finalizerService = new ExamFinalizerService(_dbContext, _cacheService, _eventPublisher);

        var clients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        clients.Group(Arg.Any<string>()).Returns(clientProxy);
        _hubContext.Clients.Returns(clients);
    }

    [Fact]
    public async Task WarnCandidate_ShouldDispatchSignalRMessage()
    {
        var exam = QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", 60, 70m);
        var studentId = Guid.CreateVersion7();
        var submission = QuizSubmission.Create(exam.Id, studentId, 60, 12345, "token123");

        await _dbContext.Exams.AddAsync(exam);
        await _dbContext.Submissions.AddAsync(submission);
        await _dbContext.SaveChangesAsync();

        var handler = new WarnCandidateCommandHandler(_dbContext, _hubContext);
        var result = await handler.Handle(new WarnCandidateCommand
        {
            SubmissionId = submission.Id,
            Message = "Please keep your eyes on the screen"
        }, CancellationToken.None);

        Assert.True(result.Success);
        _hubContext.Clients.Received(1).Group($"exam_{submission.Id}");
    }

    [Fact]
    public async Task ForceDisconnectCandidate_ShouldDisqualifyAndSendSignalR()
    {
        var exam = QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", 60, 70m);
        var studentId = Guid.CreateVersion7();
        var submission = QuizSubmission.Create(exam.Id, studentId, 60, 12345, "token123");

        await _dbContext.Exams.AddAsync(exam);
        await _dbContext.Submissions.AddAsync(submission);
        await _dbContext.SaveChangesAsync();

        var handler = new ForceDisconnectCandidateCommandHandler(_dbContext, _finalizerService, _hubContext);
        var result = await handler.Handle(new ForceDisconnectCandidateCommand
        {
            SubmissionId = submission.Id,
            Reason = "Unauthorized notes detected"
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(SubmissionStatus.Disqualified, submission.Status);
        await _cacheService.Received(1).RemoveAsync($"exam_session:{submission.Id}", Arg.Any<CancellationToken>());
        _hubContext.Clients.Received(1).Group($"exam_{submission.Id}");
    }

    [Fact]
    public async Task GetLiveCandidates_ShouldReturnCandidateMetrics()
    {
        var exam = QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", 60, 70m);
        var studentId = Guid.CreateVersion7();
        var submission = QuizSubmission.Create(exam.Id, studentId, 60, 12345, "token123", appliedRules: new ExamRuleConfig { MaxAllowedViolations = 5 });
        submission.RecordViolation("TAB_SWITCH", "Switched tabs");

        await _dbContext.Exams.AddAsync(exam);
        await _dbContext.Submissions.AddAsync(submission);
        await _dbContext.SaveChangesAsync();

        _cacheService.GetAsync<bool?>($"exam_liveness:{submission.Id}", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new GetLiveCandidatesQueryHandler(_dbContext, _cacheService, _currentUser);
        var result = await handler.Handle(new GetLiveCandidatesQuery(exam.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(studentId, result.Data[0].StudentId);
        Assert.Equal(1, result.Data[0].ViolationCount);
        Assert.True(result.Data[0].IsOnline);
    }
}
