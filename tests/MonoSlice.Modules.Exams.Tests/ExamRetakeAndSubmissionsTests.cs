using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.GetExamSubmissions;
using MonoSlice.Modules.Exams.Features.GrantRetake;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public sealed class ExamRetakeAndSubmissionsTests
{
    private static ExamsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new ExamsDbContext(options);
    }

    [Fact]
    public async Task GrantExamRetake_WhenStudentHasSubmission_RemovesLatestSubmissionAndClearsRedis()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var instructorId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();

        var exam = QuizExam.Create(
            instructorId,
            "Final Architecture Exam",
            "Test description",
            QuizMode.RealExam,
            60,
            75m,
            3,
            2);

        await db.Exams.AddAsync(exam);

        var submission = QuizSubmission.Create(
            exam.Id,
            studentId,
            60,
            12345,
            "session-token-123",
            1);

        submission.Disqualify("Multiple monitor detected");

        await db.Submissions.AddAsync(submission);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(instructorId);
        currentUser.Roles.Returns(["Instructor"]);

        var cacheService = Substitute.For<ICacheService>();
        var coursesModuleApi = Substitute.For<ICoursesModuleApi>();

        var handler = new GrantExamRetakeCommandHandler(db, currentUser, cacheService, coursesModuleApi);
        var command = new GrantExamRetakeCommand
        {
            ExamId = exam.Id,
            StudentId = studentId,
            Reason = "False positive monitor detection resolved"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Retake permission granted", result.Message);

        var remainingSubmissions = await db.Submissions
            .Where(s => s.ExamId == exam.Id && s.StudentId == studentId)
            .ToListAsync();

        Assert.Empty(remainingSubmissions);
        await cacheService.Received(1).RemoveAsync($"exam_session:{submission.Id}", Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveAsync($"exam_answers:{submission.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExamSubmissions_ReturnsSubmissionsForInstructor()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var instructorId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();

        var exam = QuizExam.Create(
            instructorId,
            "Midterm Exam",
            "Test description",
            QuizMode.RealExam,
            45,
            70m,
            3,
            1);

        await db.Exams.AddAsync(exam);

        var submission = QuizSubmission.Create(
            exam.Id,
            studentId,
            45,
            54321,
            "session-token-abc",
            1);

        submission.Complete(85m, 70m);
        await db.Submissions.AddAsync(submission);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(instructorId);
        currentUser.Roles.Returns(["Instructor"]);

        var identityApi = Substitute.For<IIdentityModuleApi>();
        identityApi.GetUsersByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new UserContractDto(studentId, "john@example.com", "John Doe", ["Student"], true, "johndoe", null)]);

        var coursesApi = Substitute.For<ICoursesModuleApi>();

        var handler = new GetExamSubmissionsQueryHandler(db, currentUser, identityApi, coursesApi);
        var query = new GetExamSubmissionsQuery
        {
            ExamId = exam.Id,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        var subDto = result.Data.Items[0];
        Assert.Equal("John Doe", subDto.StudentName);
        Assert.Equal(85m, subDto.Score);
        Assert.True(subDto.IsPassed);
        Assert.Equal("Completed", subDto.Status);
    }
}
