using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.Proctor.ForceDisconnectCandidate;

public sealed partial class ForceDisconnectCandidateCommand : ICommand<ApiResponse>
{
    public Guid SubmissionId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = "Disqualified by Proctor";
}
