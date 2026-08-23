using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.PublishExam;

public sealed partial class PublishExamCommand : ICommand<ApiResponse>
{
    public Guid Id { get; init; }
    public bool Publish { get; init; } = true;
}
