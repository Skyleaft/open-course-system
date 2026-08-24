using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Assessments.Features.GetCertificate;

public sealed class GetCertificateQueryHandler : IQueryHandler<GetCertificateQuery, ApiResponse<CertificateDetailDto>>
{
    private readonly AssessmentsDbContext _dbContext;

    public GetCertificateQueryHandler(AssessmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<CertificateDetailDto>> Handle(
        GetCertificateQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.CertificateNumber))
        {
            throw new ValidationException("Certificate number is required.");
        }

        var certificate = await _dbContext.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CertificateNumber.ToLower() == query.CertificateNumber.Trim().ToLower(), cancellationToken);

        if (certificate is null)
        {
            throw new NotFoundException(nameof(Certificate), query.CertificateNumber);
        }

        var dto = certificate.Adapt<CertificateDetailDto>() with
        {
            Status = certificate.Status.ToString()
        };

        return ApiResponse.Ok(dto);
    }
}
