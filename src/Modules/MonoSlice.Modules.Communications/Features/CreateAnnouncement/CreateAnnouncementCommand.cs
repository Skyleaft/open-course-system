using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.CreateAnnouncement;

public sealed partial class CreateAnnouncementCommand : ICommand<ApiResponse<AnnouncementDto>>
{
    public Guid? CourseId { get; init; }

    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Content { get; init; } = string.Empty;

    public bool IsPinned { get; init; } = false;
}
