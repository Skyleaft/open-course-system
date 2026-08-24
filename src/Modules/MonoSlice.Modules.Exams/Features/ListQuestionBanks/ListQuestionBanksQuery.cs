using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.ListQuestionBanks;

public sealed partial class ListQuestionBanksQuery : IQuery<ApiResponse<PaginatedList<QuestionBankSummaryDto>>>
{
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record QuestionBankSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string? Category,
    List<string> Tags,
    int QuestionCount,
    Guid CreatedBy,
    Guid? UpdatedBy,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
