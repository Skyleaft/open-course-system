using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Communications.Features.GetDiscussionThread;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.PostThreadComment;

public sealed record PostThreadCommentCommand : ICommand<ApiResponse<ThreadCommentDto>>
{
    public Guid ThreadId { get; init; }

    public Guid? ParentCommentId { get; init; }

    [Required]
    public string Content { get; init; } = string.Empty;
}
