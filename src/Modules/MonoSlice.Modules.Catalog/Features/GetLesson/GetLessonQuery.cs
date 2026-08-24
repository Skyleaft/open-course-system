using MonoSlice.Modules.Catalog.Features.AddLesson;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.GetLesson;

public sealed record GetLessonQuery(Guid LessonId) : IQuery<ApiResponse<LessonResultDto>>;
