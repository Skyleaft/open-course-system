using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.AddSection;

public sealed partial class AddSectionCommand : ICommand<ApiResponse<SectionResultDto>>
{
    public Guid CourseId { get; init; }

    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;
}

public sealed record SectionResultDto(
    Guid Id,
    Guid CourseId,
    string Title,
    int OrderIndex);
