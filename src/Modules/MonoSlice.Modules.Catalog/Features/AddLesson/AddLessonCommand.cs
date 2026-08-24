using Sannr;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.AddLesson;

public sealed partial class AddLessonCommand : ICommand<ApiResponse<LessonResultDto>>
{
    public Guid SectionId { get; init; }

    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;

    public LessonType Type { get; init; } = LessonType.Text;

    public string? ContentUrl { get; init; }

    public string? TextContent { get; init; }

    public int DurationMinutes { get; init; } = 0;
}

public sealed record LessonResultDto(
    Guid Id,
    Guid SectionId,
    string Title,
    string Type,
    string? ContentUrl,
    string? TextContent,
    int DurationMinutes,
    int OrderIndex);
