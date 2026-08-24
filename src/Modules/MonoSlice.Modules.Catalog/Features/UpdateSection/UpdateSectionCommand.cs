using MonoSlice.Modules.Catalog.Features.AddSection;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.UpdateSection;

public sealed record UpdateSectionCommand : ICommand<ApiResponse<SectionResultDto>>
{
    public Guid SectionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? OrderIndex { get; init; }
}
