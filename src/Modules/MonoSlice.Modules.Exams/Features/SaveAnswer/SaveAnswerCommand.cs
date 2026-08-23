using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.SaveAnswer;

public sealed partial class SaveAnswerCommand : ICommand<ApiResponse>
{
    public Guid SubmissionId { get; init; }
    public Guid QuestionId { get; init; }
    public List<Guid>? SelectedOptionIds { get; init; }
    public string? EssayText { get; init; }
}
