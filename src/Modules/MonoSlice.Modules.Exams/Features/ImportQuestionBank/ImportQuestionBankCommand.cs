using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ImportQuestionBank;

public sealed partial class ImportQuestionBankCommand : ICommand<ApiResponse<ImportQuestionBankResultDto>>
{
    public Stream FileStream { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public Guid? TargetBankId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
}

public sealed record ImportQuestionBankResultDto(
    Guid BankId,
    string BankTitle,
    int TotalImportedQuestions,
    List<string> Warnings);
