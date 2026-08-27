using Sannr;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public sealed partial class AddQuestionCommand : ICommand<ApiResponse<QuestionResultDto>>
{
    public Guid? BankId { get; init; }
    public Guid? ExamId { get; init; }
    public Guid? SectionId { get; init; }

    [Required]
    public string QuestionText { get; init; } = string.Empty;

    public QuestionType Type { get; init; } = QuestionType.SingleChoice;

    public GradingMethod? GradingMethod { get; init; }

    public decimal Points { get; init; } = 1m;

    public string? Explanation { get; init; }

    public string? Category { get; init; }

    public List<string> Tags { get; init; } = [];

    public List<QuestionOptionDto> Options { get; init; } = [];
}

public sealed record QuestionOptionDto(
    Guid? Id,
    string Text,
    bool IsCorrect,
    decimal Points = 0m,
    decimal PenaltyPoints = 0m);

public sealed record QuestionResultDto(
    Guid Id,
    Guid? ExamId,
    Guid? SectionId,
    string QuestionText,
    string Type,
    decimal Points,
    int OrderIndex,
    string? Explanation,
    string? Category,
    IReadOnlyList<string> Tags,
    IReadOnlyList<QuestionOptionDto> Options,
    Guid CreatedBy,
    Guid? UpdatedBy,
    DateTime CreatedAtUtc,
    Guid? BankId = null,
    string GradingMethod = nameof(Domain.GradingMethod.PartialWithPenalty));
