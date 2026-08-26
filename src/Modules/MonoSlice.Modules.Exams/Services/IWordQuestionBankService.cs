using MonoSlice.Modules.Exams.Domain;

namespace MonoSlice.Modules.Exams.Services;

public sealed record ParsedOptionItem(
    string Text,
    bool IsCorrect,
    decimal Points = 0);

public sealed record ParsedQuestionItem(
    int Number,
    string QuestionText,
    QuestionType Type,
    decimal Points,
    string? Explanation,
    List<ParsedOptionItem> Options);

public sealed record WordQuestionBankParseResult(
    string? DocumentTitle,
    string? DocumentCategory,
    List<ParsedQuestionItem> Questions,
    List<string> Warnings);

public interface IWordQuestionBankService
{
    Task<WordQuestionBankParseResult> ParseDocxAsync(Stream docxStream, CancellationToken ct = default);
    byte[] GenerateTemplateDocx();
}
