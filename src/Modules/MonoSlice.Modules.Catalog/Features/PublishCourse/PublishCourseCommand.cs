using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.PublishCourse;

public sealed partial class PublishCourseCommand : ICommand<ApiResponse>
{
    public Guid Id { get; init; }
    public bool Publish { get; init; } = true;
}
