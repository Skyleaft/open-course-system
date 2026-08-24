using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.GetDiscussionThread;

public sealed record GetDiscussionThreadByIdQuery(Guid Id) : IQuery<ApiResponse<DiscussionThreadDetailDto>>;
