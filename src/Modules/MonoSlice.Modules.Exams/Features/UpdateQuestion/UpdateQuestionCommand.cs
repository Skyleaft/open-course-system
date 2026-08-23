using Sannr;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.UpdateQuestion;

public sealed partial class UpdateQuestionCommand : ICommand<ApiResponse<QuestionResultDto>>
{
    public Guid QuestionId { get; init; }

    [Required]
    public string QuestionText { get; init; } = string.Empty;

    public QuestionType Type { get; init; } = QuestionType.SingleChoice;

    public decimal Points { get; init; } = 1m;

    public string? Explanation { get; init; }

    public List<QuestionOptionDto> Options { get; init; } = [];
}
