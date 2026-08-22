using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.SubmitAssignment;

public sealed record SubmitAssignmentCommand : ICommand<ApiResponse<SubmissionResultDto>>
{
    public Guid AssignmentId { get; init; }

    [Required]
    public string FileUrl { get; init; } = string.Empty;
}

public sealed record SubmissionResultDto(
    Guid SubmissionId,
    Guid AssignmentId,
    Guid StudentId,
    string FileUrl,
    DateTime SubmittedAtUtc);
