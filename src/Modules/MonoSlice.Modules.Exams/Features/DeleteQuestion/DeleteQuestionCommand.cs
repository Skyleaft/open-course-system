using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.DeleteQuestion;

public sealed record DeleteQuestionCommand(Guid QuestionId) : ICommand<ApiResponse<bool>>;
