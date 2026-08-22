using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Assessments.Features.GetCertificate;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.IssueCertificate;

public sealed record IssueCertificateCommand : ICommand<ApiResponse<CertificateDetailDto>>
{
    [Required]
    public Guid StudentId { get; init; }

    [Required]
    public Guid CourseId { get; init; }

    [Range(0, 100)]
    public decimal FinalScore { get; init; }
}
