using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.GetCertificate;

public sealed record GetCertificateQuery(string CertificateNumber) : IQuery<ApiResponse<CertificateDetailDto>>;

public sealed record CertificateDetailDto(
    Guid Id,
    string CertificateNumber,
    Guid StudentId,
    Guid CourseId,
    decimal FinalScore,
    string CertificateHash,
    string Status,
    DateTime IssuedAtUtc,
    string? RevocationReason);
