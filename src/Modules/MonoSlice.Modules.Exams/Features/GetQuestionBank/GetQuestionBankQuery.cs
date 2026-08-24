using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.GetQuestionBank;

public sealed partial class GetQuestionBankQuery : IQuery<ApiResponse<QuestionBankDetailDto>>
{
    public Guid Id { get; init; }

    public GetQuestionBankQuery() { }

    public GetQuestionBankQuery(Guid id)
    {
        Id = id;
    }
}

public sealed record QuestionBankDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? Category,
    List<string> Tags,
    Guid CreatedBy,
    Guid? UpdatedBy,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    List<BankQuestionDto> Questions
);

public sealed record BankQuestionDto(
    Guid Id,
    Guid BankId,
    string QuestionText,
    string Type,
    decimal Points,
    int OrderIndex,
    string? Explanation,
    List<QuestionOption> Options
);
