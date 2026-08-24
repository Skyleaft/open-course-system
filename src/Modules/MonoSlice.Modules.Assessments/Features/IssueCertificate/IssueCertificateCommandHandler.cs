using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Features.GetCertificate;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Assessments.Features.IssueCertificate;

public sealed class IssueCertificateCommandHandler : ICommandHandler<IssueCertificateCommand, ApiResponse<CertificateDetailDto>>
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public IssueCertificateCommandHandler(
        AssessmentsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<CertificateDetailDto>> Handle(
        IssueCertificateCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to issue certificates.");
        }

        var existing = await _dbContext.Certificates
            .FirstOrDefaultAsync(c => c.StudentId == command.StudentId && c.CourseId == command.CourseId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == CertificateStatus.Revoked)
            {
                throw new BusinessRuleException("A revoked certificate already exists for this student and course.");
            }

            var existingDto = existing.Adapt<CertificateDetailDto>() with
            {
                Status = existing.Status.ToString()
            };
            return ApiResponse.Ok(existingDto, "Certificate already exists for this course.");
        }

        var certificate = Certificate.Issue(
            command.StudentId,
            command.CourseId,
            command.FinalScore);

        await _dbContext.Certificates.AddAsync(certificate, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = certificate.Adapt<CertificateDetailDto>() with
        {
            Status = certificate.Status.ToString()
        };

        return ApiResponse.Ok(dto, "Certificate issued successfully.");
    }
}
