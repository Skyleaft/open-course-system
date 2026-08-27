using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.BroadcastExamMessage;

public sealed record BroadcastExamMessageCommand(
    Guid ExamId,
    string Message) : ICommand<ApiResponse>;
