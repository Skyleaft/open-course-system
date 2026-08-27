using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ListQuestions;

public sealed partial class ListQuestionsQuery : IQuery<ApiResponse<PaginatedList<QuestionItemDto>>>
{
    public Guid? BankId { get; init; }
    public string? SearchTerm { get; init; }
    public QuestionType? Type { get; init; }
    public string? Category { get; init; }
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record QuestionItemDto(
    Guid Id,
    Guid BankId,
    string BankTitle,
    string? BankCategory,
    string QuestionText,
    string Type,
    decimal Points,
    int OrderIndex,
    string? Explanation,
    List<QuestionOption> Options,
    DateTime CreatedAtUtc,
    string GradingMethod = nameof(Domain.GradingMethod.PartialWithPenalty)
);
