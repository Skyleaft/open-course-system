using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.DeleteExam;

public sealed record DeleteExamCommand(Guid ExamId) : ICommand<ApiResponse<bool>>;
