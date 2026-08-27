using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public sealed class AddQuestionCommandHandler : ICommandHandler<AddQuestionCommand, ApiResponse<QuestionResultDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AddQuestionCommandHandler(ExamsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<QuestionResultDto>> Handle(
        AddQuestionCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to add questions.");
        }

        var userId = _currentUser.UserId.Value;

        // 1. Resolve or create target QuestionBank package
        QuestionBank? bank = null;
        if (command.BankId.HasValue)
        {
            bank = await _dbContext.QuestionBanks
                .Include(b => b.Questions)
                .FirstOrDefaultAsync(b => b.Id == command.BankId.Value, cancellationToken);

            if (bank is null)
            {
                throw new NotFoundException(nameof(QuestionBank), command.BankId.Value);
            }
        }
        else
        {
            // Find existing bank with same category or create a default bank
            var bankTitle = !string.IsNullOrWhiteSpace(command.Category) ? $"{command.Category} Question Pool" : "General Question Bank";
            bank = await _dbContext.QuestionBanks
                .Include(b => b.Questions)
                .FirstOrDefaultAsync(b => b.CreatedBy == userId && b.Title == bankTitle, cancellationToken);

            if (bank is null)
            {
                bank = QuestionBank.Create(
                    userId,
                    bankTitle,
                    description: "Auto-managed Question Bank",
                    category: command.Category,
                    tags: command.Tags);

                await _dbContext.QuestionBanks.AddAsync(bank, cancellationToken);
            }
        }

        var options = command.Options.Select(o => new QuestionOption(
            o.Id ?? Guid.CreateVersion7(),
            o.Text,
            o.IsCorrect,
            o.Points,
            o.PenaltyPoints
        )).ToList();

        var bankQuestion = bank.AddQuestion(
            command.QuestionText,
            command.Type,
            command.Points,
            command.Explanation,
            options,
            command.GradingMethod);

        // 2. If ExamId or SectionId provided, ensure QuizSection links to this QuestionBank
        Guid? sectionId = command.SectionId;
        if (command.ExamId.HasValue || command.SectionId.HasValue)
        {
            if (command.SectionId.HasValue)
            {
                var targetSection = await _dbContext.Sections
                    .FirstOrDefaultAsync(s => s.Id == command.SectionId.Value, cancellationToken);

                if (targetSection is null)
                {
                    throw new NotFoundException(nameof(QuizSection), command.SectionId.Value);
                }
            }
            else if (command.ExamId.HasValue)
            {
                var exam = await _dbContext.Exams
                    .Include(e => e.Sections)
                    .FirstOrDefaultAsync(e => e.Id == command.ExamId.Value, cancellationToken);

                if (exam is null)
                {
                    throw new NotFoundException(nameof(QuizExam), command.ExamId.Value);
                }

                // Check if exam already has a section linked to this bank
                var existingSection = exam.Sections.FirstOrDefault(s => s.QuestionBankId == bank.Id);
                if (existingSection is null)
                {
                    var newSec = exam.AddSection(bank.Id, bank.Title);
                    sectionId = newSec.Id;
                }
                else
                {
                    sectionId = existingSection.Id;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new QuestionResultDto(
            bankQuestion.Id,
            command.ExamId,
            sectionId,
            bankQuestion.QuestionText,
            bankQuestion.Type.ToString(),
            bankQuestion.Points,
            bankQuestion.OrderIndex,
            bankQuestion.Explanation,
            bank.Category,
            bank.Tags,
            bankQuestion.Options.Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect, o.Points, o.PenaltyPoints)).ToList(),
            bank.CreatedBy,
            bank.UpdatedBy,
            bank.CreatedAtUtc,
            bank.Id,
            bankQuestion.GradingMethod.ToString());

        return ApiResponse.Ok(result, "Question added to bank successfully.");
    }
}
