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
        var exam = QuizExam.Create(
            instructorId,
            "C# Certification Exam",
            "Professional C# assessment",
            QuizMode.RealExam,
            durationMinutes: 90,
            passingScore: 75m,
            maxAllowedViolations: 3);

        Assert.NotEqual(Guid.Empty, exam.Id);
        Assert.Equal(instructorId, exam.InstructorId);
        Assert.Equal("C# Certification Exam", exam.Title);
        Assert.Equal(QuizMode.RealExam, exam.Mode);
        Assert.Equal(90, exam.DurationMinutes);
        Assert.Equal(75m, exam.PassingScore);
        Assert.Equal(3, exam.MaxAllowedViolations);
        Assert.False(exam.IsPublished);
        Assert.Empty(exam.Questions);
    }

    [Fact]
    public void Publish_ShouldThrowException_WhenNoQuestionsExist()
    {
        var exam = QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", QuizMode.Simulation, 30, 60m);

        Assert.Throws<BusinessRuleException>(() => exam.Publish());
    }

    [Fact]
    public void AddQuestion_AndPublish_ShouldSucceed()
    {
        var exam = QuizExam.Create(Guid.CreateVersion7(), "Exam 1", "Desc", QuizMode.Simulation, 30, 60m);

        var options = new List<QuestionOption>
        {
            new(Guid.CreateVersion7(), "Option A", true),
            new(Guid.CreateVersion7(), "Option B", false)
        };

        var question = exam.AddQuestion(
            "What is CLR?",
            QuestionType.SingleChoice,
            points: 5m,
            explanation: "Common Language Runtime",
            options);

        Assert.Single(exam.Questions);
        Assert.Equal("What is CLR?", question.QuestionText);
        Assert.Equal(5m, question.Points);

        exam.Publish();
        Assert.True(exam.IsPublished);
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
            activeSessionToken: "token123");

        submission.RecordViolation("TAB_SWITCH", "User switched browser tab", maxAllowedViolations: 2);
        Assert.Equal(SubmissionStatus.InProgress, submission.Status);

        submission.RecordViolation("FULLSCREEN_EXIT", "User exited full screen", maxAllowedViolations: 2);
        Assert.Equal(SubmissionStatus.Disqualified, submission.Status);
    }
}
