using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.DetachExam;

public sealed partial class DetachExamCommand : ICommand<ApiResponse<bool>>
{
    public Guid CourseId { get; init; }

    [Required]
    public Guid ExamId { get; init; }
}
