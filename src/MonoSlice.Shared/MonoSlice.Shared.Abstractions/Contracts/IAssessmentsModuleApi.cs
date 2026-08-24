namespace MonoSlice.Shared.Abstractions.Contracts;

public interface IAssessmentsModuleApi
{
    Task<IReadOnlyList<GradeRecordDto>> GetStudentGradeRecordsAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<CertificateDto?> GetStudentCertificateAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<CertificateDto> IssueCertificateAsync(
        Guid studentId,
        Guid courseId,
        decimal finalScore,
        CancellationToken cancellationToken = default);
}

public sealed record GradeRecordDto(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    string ItemType,
    Guid ReferenceId,
    decimal Score,
    decimal MaxScore,
    decimal WeightPercentage,
    DateTime EvaluatedAtUtc);

public sealed record CertificateDto(
    Guid Id,
    string CertificateNumber,
    Guid StudentId,
    Guid CourseId,
    decimal FinalScore,
    string CertificateHash,
    string Status,
    DateTime IssuedAtUtc);
