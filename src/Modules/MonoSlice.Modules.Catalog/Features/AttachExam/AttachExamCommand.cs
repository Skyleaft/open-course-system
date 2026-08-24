using Sannr;
using MonoSlice.Modules.Catalog.Features.GetCourse;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.AttachExam;

public sealed partial class AttachExamCommand : ICommand<ApiResponse<CourseExamDto>>
{
    public Guid CourseId { get; init; }

    [Required]
    public Guid ExamId { get; init; }

    public int OrderIndex { get; init; } = 1;

    public bool IsMandatory { get; init; } = true;
}
