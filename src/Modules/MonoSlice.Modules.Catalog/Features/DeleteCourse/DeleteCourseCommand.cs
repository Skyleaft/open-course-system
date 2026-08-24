using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.DeleteCourse;

public sealed record DeleteCourseCommand(Guid Id) : ICommand<ApiResponse>;
