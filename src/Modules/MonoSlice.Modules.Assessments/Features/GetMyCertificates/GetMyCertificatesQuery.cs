using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.GetMyCertificates;

public sealed record GetMyCertificatesQuery : IQuery<ApiResponse<IReadOnlyList<StudentCertificateDto>>>;

public sealed record StudentCertificateDto(
    Guid Id,
    string CertificateNumber,
    Guid StudentId,
    Guid CourseId,
    decimal FinalScore,
    string CertificateHash,
    string Status,
    DateTime IssuedAtUtc);
