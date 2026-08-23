using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.WarnCandidate;

public sealed partial class WarnCandidateCommand : ICommand<ApiResponse>
{
    public Guid SubmissionId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Message { get; init; } = string.Empty;
}
