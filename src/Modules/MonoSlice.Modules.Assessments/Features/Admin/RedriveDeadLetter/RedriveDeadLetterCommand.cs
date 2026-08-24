using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.Admin.RedriveDeadLetter;

public sealed partial class RedriveDeadLetterCommand : ICommand<ApiResponse>
{
    public Guid Id { get; init; }
}
