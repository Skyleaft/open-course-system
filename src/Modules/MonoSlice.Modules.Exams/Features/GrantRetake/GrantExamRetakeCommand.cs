using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Exams.Features.GrantRetake;

public sealed partial class GrantExamRetakeCommand : ICommand<ApiResponse<bool>>
{
    [Required]
    public Guid ExamId { get; init; }

    [Required]
    public Guid StudentId { get; init; }

    public string? Reason { get; init; }
}
