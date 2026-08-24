using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Assessments.Features.VerifyCertificate;

public sealed class VerifyCertificateQueryHandler : IQueryHandler<VerifyCertificateQuery, ApiResponse<CertificateVerificationDto>>
{
    private readonly AssessmentsDbContext _dbContext;

    public VerifyCertificateQueryHandler(AssessmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<CertificateVerificationDto>> Handle(
        VerifyCertificateQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.CertificateHash))
        {
            throw new ValidationException("Certificate hash is required.");
        }

        var normalizedHash = query.CertificateHash.Trim().ToLowerInvariant();

        var certificate = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CertificateHash.ToLower() == normalizedHash, cancellationToken);

        if (certificate is null)
        {
            return ApiResponse.Ok(new CertificateVerificationDto(
                false,
                string.Empty,
                Guid.Empty,
                Guid.Empty,
                0m,
                "NotFound",
                DateTime.MinValue,
                query.CertificateHash), "Certificate not found or hash is invalid.");
        }

        var isCryptographicallyValid = certificate.VerifyAuthenticity();
        var isValid = isCryptographicallyValid && certificate.Status == CertificateStatus.Issued;

        var dto = certificate.Adapt<CertificateVerificationDto>() with
        {
            IsValid = isValid,
            Status = certificate.Status.ToString()
        };

        return ApiResponse.Ok(dto, isValid ? "Certificate successfully verified." : "Certificate is revoked or modified.");
    }
}
