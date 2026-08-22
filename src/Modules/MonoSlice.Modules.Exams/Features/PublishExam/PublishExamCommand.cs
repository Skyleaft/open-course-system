using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.PublishExam;

public sealed record PublishExamCommand(Guid Id, bool Publish = true) : ICommand<ApiResponse>;
