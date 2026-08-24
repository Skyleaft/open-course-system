using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.CloseDiscussionThread;

public sealed partial class CloseDiscussionThreadCommand : ICommand<ApiResponse<DiscussionThreadSummaryDto>>
{
    public Guid ThreadId { get; init; }
}
