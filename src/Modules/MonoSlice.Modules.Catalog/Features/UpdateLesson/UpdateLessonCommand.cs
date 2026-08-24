using Sannr;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.AddLesson;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.UpdateLesson;

public sealed partial class UpdateLessonCommand : ICommand<ApiResponse<LessonResultDto>>
{
    public Guid LessonId { get; init; }

    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;

    public LessonType Type { get; init; } = LessonType.Text;

    public string? ContentUrl { get; init; }

    public string? TextContent { get; init; }

    public int DurationMinutes { get; init; } = 0;

    public int? OrderIndex { get; init; }
}
