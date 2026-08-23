using Sannr;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.AddQuestion;

public sealed partial class AddQuestionCommand : ICommand<ApiResponse<QuestionResultDto>>
{
    public Guid ExamId { get; init; }

    [Required]
    public string QuestionText { get; init; } = string.Empty;

    public QuestionType Type { get; init; } = QuestionType.SingleChoice;

    public decimal Points { get; init; } = 1m;

    public string? Explanation { get; init; }

    public List<QuestionOptionDto> Options { get; init; } = [];
}

public sealed record QuestionOptionDto(
    Guid? Id,
    string Text,
    bool IsCorrect);

public sealed record QuestionResultDto(
    Guid Id,
    Guid ExamId,
    string QuestionText,
    string Type,
    decimal Points,
    int OrderIndex,
    string? Explanation,
    IReadOnlyList<QuestionOptionDto> Options);
