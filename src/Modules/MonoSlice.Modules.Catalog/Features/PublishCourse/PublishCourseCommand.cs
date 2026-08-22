using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.PublishCourse;

public sealed record PublishCourseCommand(Guid Id, bool Publish = true) : ICommand<ApiResponse>;
