using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.CloseDiscussionThread;

public sealed record CloseDiscussionThreadCommand(Guid ThreadId) : ICommand<ApiResponse<DiscussionThreadSummaryDto>>;
