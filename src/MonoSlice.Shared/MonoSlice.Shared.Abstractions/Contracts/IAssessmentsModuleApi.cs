namespace MonoSlice.Shared.Abstractions.Contracts;

public record CertificateContractDto(
    Guid Id,
    string CertificateNumber,
    Guid StudentId,
    Guid CourseId,
    decimal FinalScore,
    string CertificateHash,
    string Status,
    DateTime IssuedAtUtc);

public interface IAssessmentsModuleApi
{
    Task<CertificateContractDto?> GetCertificateByHashAsync(string certificateHash, CancellationToken ct = default);
    Task<bool> RecordGradeAsync(
        Guid studentId,
        Guid courseId,
        string itemType,
        Guid referenceId,
        decimal score,
        decimal maxScore,
        decimal weightPercentage,
        CancellationToken ct = default);
}
