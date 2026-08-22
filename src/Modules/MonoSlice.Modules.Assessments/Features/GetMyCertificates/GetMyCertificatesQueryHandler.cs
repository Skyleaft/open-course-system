using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Assessments.Features.GetMyCertificates;

public sealed class GetMyCertificatesQueryHandler : IQueryHandler<GetMyCertificatesQuery, ApiResponse<IReadOnlyList<StudentCertificateDto>>>
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyCertificatesQueryHandler(
        AssessmentsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<StudentCertificateDto>>> Handle(
        GetMyCertificatesQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var studentId = _currentUser.UserId.Value;

        var certs = await _dbContext.Certificates
            .AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.IssuedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = certs.Select(c => c.Adapt<StudentCertificateDto>() with
        {
            Status = c.Status.ToString()
        }).ToList();

        return ApiResponse.Ok<IReadOnlyList<StudentCertificateDto>>(dtos);
    }
}
