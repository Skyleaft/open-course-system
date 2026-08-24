using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Assessments.Contracts;

public sealed class AssessmentsModuleApi : IAssessmentsModuleApi
{
    private readonly AssessmentsDbContext _dbContext;

    public AssessmentsModuleApi(AssessmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GradeRecordDto>> GetStudentGradeRecordsAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.GradeRecords
            .AsNoTracking()
            .Where(g => g.StudentId == studentId && g.CourseId == courseId)
            .OrderByDescending(g => g.EvaluatedAtUtc)
            .ToListAsync(cancellationToken);

        return records.Select(g => g.Adapt<GradeRecordDto>() with
        {
            ItemType = g.ItemType.ToString()
        }).ToList();
    }

    public async Task<CertificateDto?> GetStudentCertificateAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var cert = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId, cancellationToken);

        if (cert is null)
        {
            return null;
        }

        return cert.Adapt<CertificateDto>() with
        {
            Status = cert.Status.ToString()
        };
    }

    public async Task<CertificateDto> IssueCertificateAsync(
        Guid studentId,
        Guid courseId,
        decimal finalScore,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.CourseId == courseId, cancellationToken);

        if (existing is not null)
        {
            return existing.Adapt<CertificateDto>() with
            {
                Status = existing.Status.ToString()
            };
        }

        var cert = Certificate.Issue(studentId, courseId, finalScore);
        await _dbContext.Certificates.AddAsync(cert, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return cert.Adapt<CertificateDto>() with
        {
            Status = cert.Status.ToString()
        };
    }
}
