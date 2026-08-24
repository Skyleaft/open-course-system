using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Modules.Exams.Services;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.ImportQuestionBank;

public sealed class ImportQuestionBankCommandHandler : ICommandHandler<ImportQuestionBankCommand, ApiResponse<ImportQuestionBankResultDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly IWordQuestionBankService _wordService;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;

    public ImportQuestionBankCommandHandler(
        ExamsDbContext dbContext,
        IWordQuestionBankService wordService,
        ICurrentUser currentUser,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _wordService = wordService;
        _currentUser = currentUser;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<ImportQuestionBankResultDto>> Handle(
        ImportQuestionBankCommand command,
        CancellationToken cancellationToken)
    {
        var parseResult = await _wordService.ParseDocxAsync(command.FileStream, cancellationToken);
        if (parseResult.Questions.Count == 0)
        {
            return ApiResponse.Fail<ImportQuestionBankResultDto>(
                parseResult.Warnings.FirstOrDefault() ?? "No valid questions were found in the Word document. Please ensure questions are numbered (e.g. 1. Question) with choices (A. Option, B. Option).",
                400);
        }

        QuestionBank bank;
        if (command.TargetBankId.HasValue && command.TargetBankId.Value != Guid.Empty)
        {
            var existingBank = await _dbContext.QuestionBanks
                .Include(b => b.Questions)
                .FirstOrDefaultAsync(b => b.Id == command.TargetBankId.Value, cancellationToken);

            if (existingBank is null)
            {
                throw new NotFoundException(nameof(QuestionBank), command.TargetBankId.Value);
            }

            if (existingBank.CreatedBy != _currentUser.UserId && !_currentUser.IsInRole("Admin"))
            {
                throw new UnauthorizedException("You do not have permission to modify this Question Bank.");
            }

            bank = existingBank;
        }
        else
        {
            var title = !string.IsNullOrWhiteSpace(command.Title)
                ? command.Title.Trim()
                : !string.IsNullOrWhiteSpace(parseResult.DocumentTitle)
                    ? parseResult.DocumentTitle.Trim()
                    : Path.GetFileNameWithoutExtension(command.FileName);

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Imported Question Bank";
            }

            var category = !string.IsNullOrWhiteSpace(command.Category)
                ? command.Category.Trim()
                : parseResult.DocumentCategory?.Trim();

            if (!_currentUser.UserId.HasValue)
            {
                throw new UnauthorizedException("Authenticated user ID is required.");
            }

            bank = QuestionBank.Create(
                _currentUser.UserId.Value,
                title,
                command.Description,
                category,
                command.Tags);

            await _dbContext.QuestionBanks.AddAsync(bank, cancellationToken);
        }

        foreach (var q in parseResult.Questions)
        {
            var options = q.Options.Select(o => new QuestionOption(
                Guid.CreateVersion7(),
                o.Text,
                o.IsCorrect
            ));

            bank.AddQuestion(
                q.QuestionText,
                q.Type,
                q.Points > 0 ? q.Points : 1m,
                q.Explanation,
                options);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync($"question-bank:{bank.Id}", cancellationToken);

        var resultDto = new ImportQuestionBankResultDto(
            bank.Id,
            bank.Title,
            parseResult.Questions.Count,
            parseResult.Warnings);

        return ApiResponse.Ok(resultDto, $"Successfully imported {parseResult.Questions.Count} questions from Word document.");
    }
}
