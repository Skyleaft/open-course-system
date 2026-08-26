using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class QuizExamDomainTests
{
    [Fact]
    public void Create_ShouldInitializeWithGuidV7AndCorrectValues()
    {
        var instructorId = Guid.CreateVersion7();
        var ruleConfig = ExamRuleConfig.StrictProctored();
        var exam = QuizExam.Create(
            instructorId,
            "C# Certification Exam",
            "Professional C# assessment",
            durationMinutes: 90,
            passingScore: 75m,
            ruleConfig: ruleConfig);

        Assert.NotEqual(Guid.Empty, exam.Id);
        Assert.Equal(instructorId, exam.InstructorId);
        Assert.Equal(instructorId, exam.CreatedBy);
        Assert.Equal("C# Certification Exam", exam.Title);
        Assert.Equal("Strict Proctored", exam.RuleConfig.Name);
        Assert.Equal(90, exam.DurationMinutes);
        Assert.Equal(75m, exam.PassingScore);
        Assert.Equal(3, exam.RuleConfig.MaxAllowedViolations);
        Assert.False(exam.IsPublished);
        Assert.Empty(exam.Sections);
    }

    [Fact]
    public void Publish_ShouldThrowException_WhenNoSectionsExist()
    {
        var exam = QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", 30, 60m, ruleConfig: ExamRuleConfig.Practice());

        Assert.Throws<BusinessRuleException>(() => exam.Publish());
    }

    [Fact]
    public void AddSection_WithQuestionBank_AndPublish_ShouldSucceed()
    {
        var instructorId = Guid.CreateVersion7();
        var exam = QuizExam.Create(instructorId, "Exam 1", "Desc", 30, 60m, ruleConfig: ExamRuleConfig.Practice());

        var qb = QuestionBank.Create(instructorId, "C# Basics Bank", "Fundamentals");
        var q = qb.AddQuestion("What is CLR?", QuestionType.SingleChoice, 5m);

        var section = exam.AddSection(qb.Id, "Section 1 - Basics", pointsOverride: 10m);

        Assert.Single(exam.Sections);
        Assert.Equal(qb.Id, section.QuestionBankId);
        Assert.Equal(10m, section.PointsOverride);
        Assert.Single(qb.Questions);

        exam.Publish();
        Assert.True(exam.IsPublished);
    }

    [Fact]
    public void QuestionBank_CreateAndUpdate_ShouldWorkCorrectly()
    {
        var authorId = Guid.CreateVersion7();
        var qb = QuestionBank.Create(
            authorId,
            "C# Pool",
            "Collection of C# questions",
            category: "C#",
            tags: ["dotnet", "runtime"]);

        var q = qb.AddQuestion("What is CLR?", QuestionType.SingleChoice, points: 5m, explanation: "Common Language Runtime", options: [new(Guid.CreateVersion7(), "Option A", true)]);

        Assert.Equal("C# Pool", qb.Title);
        Assert.Equal(authorId, qb.CreatedBy);
        Assert.Equal("C#", qb.Category);
        Assert.Contains("dotnet", qb.Tags);
        Assert.Single(qb.Questions);
        Assert.Equal("What is CLR?", q.QuestionText);

        var updaterId = Guid.CreateVersion7();
        qb.Update(updaterId, "Advanced C# Pool", category: "Advanced C#");
        qb.UpdateQuestion(q.Id, "Updated Question", QuestionType.SingleChoice, 10m);

        Assert.Equal("Advanced C# Pool", qb.Title);
        Assert.Equal(updaterId, qb.UpdatedBy);
        Assert.Equal("Updated Question", q.QuestionText);
        Assert.Equal(10m, q.Points);
    }

    [Fact]
    public void Submission_ShouldEnforceMaxAllowedViolationsAndDisqualify()
    {
        var examId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();

        var submission = QuizSubmission.Create(
            examId,
            studentId,
            durationMinutes: 60,
            randomSeed: 12345,
            activeSessionToken: "token123",
            appliedRules: new ExamRuleConfig { MaxAllowedViolations = 2, AutoDisqualifyOnExceed = true });

        submission.RecordViolation("TAB_SWITCH", "User switched browser tab");
        Assert.Equal(SubmissionStatus.InProgress, submission.Status);

        submission.RecordViolation("FULLSCREEN_EXIT", "User exited full screen");
        Assert.Equal(SubmissionStatus.Disqualified, submission.Status);
    }

    [Fact]
    public void Submission_ShouldCapMaxAllowedEndTime_WhenAvailableToUtcIsEarlierThanNaturalDuration()
    {
        var examId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();
        var deadline = DateTime.UtcNow.AddMinutes(15);

        var submission = QuizSubmission.Create(
            examId,
            studentId,
            durationMinutes: 60,
            randomSeed: 12345,
            activeSessionToken: "token123",
            attemptNumber: 1,
            availableToUtc: deadline);

        Assert.Equal(deadline, submission.MaxAllowedEndTimeUtc);
    }

    [Fact]
    public void QuizExam_InvalidAvailableDates_ShouldThrowException()
    {
        var from = DateTime.UtcNow.AddDays(2);
        var to = DateTime.UtcNow.AddDays(1);

        Assert.Throws<BusinessRuleException>(() =>
            QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", 60, 70m, availableFromUtc: from, availableToUtc: to));
    }
}
