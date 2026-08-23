using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.DeleteLesson;

public sealed record DeleteLessonCommand(Guid LessonId) : ICommand<ApiResponse>;
