using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.GetDiscussionThreads;

public sealed record GetDiscussionThreadsQuery(
    Guid? CourseId = null,
    Guid? LessonId = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<ApiResponse<PaginatedList<DiscussionThreadSummaryDto>>>;
