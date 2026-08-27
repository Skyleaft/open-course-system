using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.CreateDiscussionThread;

public sealed partial class CreateDiscussionThreadCommand : ICommand<ApiResponse<DiscussionThreadSummaryDto>>
{
    [Required]
    public Guid CourseId { get; init; }

    public Guid? LessonId { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(50000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}
