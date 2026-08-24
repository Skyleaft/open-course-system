using System.Security.Cryptography;
using System.Text;
using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Assessments.Domain;

public sealed class Certificate : AggregateRoot<Guid>
{
    public string CertificateNumber { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public decimal FinalScore { get; private set; }
    public string CertificateHash { get; private set; } = string.Empty;
    public CertificateStatus Status { get; private set; } = CertificateStatus.Issued;
    public DateTime IssuedAtUtc { get; private set; } = DateTime.UtcNow;
    public string? RevocationReason { get; private set; }

    private Certificate() : base(Guid.CreateVersion7()) { }

    public static Certificate Issue(
        Guid studentId,
        Guid courseId,
        decimal finalScore,
        string? customCertNumber = null)
    {
        if (studentId == Guid.Empty)
        {
            throw new ValidationException("Student ID is required.");
        }

        if (courseId == Guid.Empty)
        {
            throw new ValidationException("Course ID is required.");
        }

        if (finalScore < 0 || finalScore > 100)
        {
            throw new BusinessRuleException("Final score must be between 0 and 100.");
        }

        var id = Guid.CreateVersion7();
        var issuedAtUtc = DateTime.UtcNow;
        var certNumber = customCertNumber ?? $"CERT-{issuedAtUtc:yyyyMMdd}-{id.ToString("N")[..8].ToUpperInvariant()}";
        var hash = ComputeHash(certNumber, studentId, courseId, finalScore, issuedAtUtc);

        return new Certificate
        {
            Id = id,
            CertificateNumber = certNumber,
            StudentId = studentId,
            CourseId = courseId,
            FinalScore = finalScore,
            CertificateHash = hash,
            Status = CertificateStatus.Issued,
            IssuedAtUtc = issuedAtUtc
        };
    }

    public static string ComputeHash(
        string certificateNumber,
        Guid studentId,
        Guid courseId,
        decimal finalScore,
        DateTime issuedAtUtc)
    {
        var rawData = $"{certificateNumber}|{studentId}|{courseId}|{finalScore:F2}|{issuedAtUtc:O}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexStringLower(hashBytes);
    }

    public bool VerifyAuthenticity()
    {
        var expectedHash = ComputeHash(CertificateNumber, StudentId, CourseId, FinalScore, IssuedAtUtc);
        return string.Equals(CertificateHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    public void Revoke(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("Revocation reason is required.");
        }

        Status = CertificateStatus.Revoked;
        RevocationReason = reason.Trim();
    }
}
