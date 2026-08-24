using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.VerifyCertificate;

public sealed record VerifyCertificateQuery(string CertificateHash) : IQuery<ApiResponse<CertificateVerificationDto>>;

public sealed record CertificateVerificationDto(
    bool IsValid,
    string CertificateNumber,
    Guid StudentId,
    Guid CourseId,
    decimal FinalScore,
    string Status,
    DateTime IssuedAtUtc,
    string CertificateHash);
