using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.ImportQuestionBank;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Modules.Exams.Services;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class WordQuestionBankServiceTests
{
    private readonly WordQuestionBankService _service = new();

    [Fact]
    public void GenerateTemplateDocx_ShouldReturnValidNonEmptyByteArray()
    {
        // Act
        var docxBytes = _service.GenerateTemplateDocx();

        // Assert
        Assert.NotNull(docxBytes);
        Assert.True(docxBytes.Length > 0);
    }

    [Fact]
    public async Task ParseDocxAsync_ShouldParseGeneratedTemplateQuestions()
    {
        // Arrange
        var docxBytes = _service.GenerateTemplateDocx();
        using var stream = new MemoryStream(docxBytes);

        // Act
        var result = await _service.ParseDocxAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Questions.Count);

        // Question 1: Single Choice (Queue FIFO)
        var q1 = result.Questions[0];
        Assert.Contains("First-In-First-Out", q1.QuestionText);
        Assert.Equal(QuestionType.SingleChoice, q1.Type);
        Assert.Equal(4, q1.Options.Count);
        Assert.True(q1.Options.First(o => o.Text.Contains("Queue")).IsCorrect);

        // Question 2: Single Choice (*A. O(log n))
        var q2 = result.Questions[1];
        Assert.Contains("Binary Search Tree", q2.QuestionText);
        Assert.Equal(QuestionType.SingleChoice, q2.Type);
        Assert.True(q2.Options.First(o => o.Text.Contains("O(log n)")).IsCorrect);

        // Question 3: Multiple Choice (A, C)
        var q3 = result.Questions[2];
        Assert.Contains("HTTP status codes", q3.QuestionText);
        Assert.Equal(QuestionType.MultipleChoice, q3.Type);
        Assert.Equal(2, q3.Options.Count(o => o.IsCorrect));

        // Question 4: True / False
        var q4 = result.Questions[3];
        Assert.Contains("foreign key", q4.QuestionText);
        Assert.Equal(QuestionType.TrueFalse, q4.Type);
        Assert.True(q4.Options.First(o => o.Text.Equals("False", StringComparison.OrdinalIgnoreCase)).IsCorrect);

        // Question 5: Essay
        var q5 = result.Questions[4];
        Assert.Contains("ACID", q5.QuestionText);
        Assert.Equal(QuestionType.Essay, q5.Type);
        Assert.Empty(q5.Options);
    }

    [Fact]
    public async Task ImportQuestionBankCommandHandler_ShouldImportQuestionsFromDocx()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ExamsDbContext(options);

        var currentUser = Substitute.For<ICurrentUser>();
        var instructorId = Guid.NewGuid();
        currentUser.UserId.Returns(instructorId);
        currentUser.IsInRole("Instructor").Returns(true);

        var cacheService = Substitute.For<ICacheService>();

        var handler = new ImportQuestionBankCommandHandler(dbContext, _service, currentUser, cacheService);

        var docxBytes = _service.GenerateTemplateDocx();
        using var stream = new MemoryStream(docxBytes);

        var command = new ImportQuestionBankCommand
        {
            FileStream = stream,
            FileName = "TestTemplate.docx",
            Title = "Computer Science Fundamentals",
            Category = "Engineering"
        };

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(5, response.Data.TotalImportedQuestions);

        var bank = await dbContext.QuestionBanks
            .Include(b => b.Questions)
            .FirstOrDefaultAsync(b => b.Id == response.Data.BankId);

        Assert.NotNull(bank);
        Assert.Equal("Computer Science Fundamentals", bank.Title);
        Assert.Equal("Engineering", bank.Category);
        Assert.Equal(5, bank.Questions.Count);
    }
}
