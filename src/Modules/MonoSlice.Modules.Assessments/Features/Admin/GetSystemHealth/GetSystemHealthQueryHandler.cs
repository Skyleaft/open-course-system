using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Assessments.Features.Admin.GetSystemHealth;

public sealed class GetSystemHealthQueryHandler : IQueryHandler<GetSystemHealthQuery, ApiResponse<SystemHealthDto>>
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetSystemHealthQueryHandler(
        AssessmentsDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<SystemHealthDto>> Handle(GetSystemHealthQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = "cache:dashboard:admin:system-health";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var unresolvedCount = await _dbContext.GradingDeadLetters
                .AsNoTracking()
                .CountAsync(d => !d.IsResolved, cancellationToken);

            var totalDlqCount = await _dbContext.GradingDeadLetters
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var totalCerts = await _dbContext.Certificates
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var totalGrades = await _dbContext.GradeRecords
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var recentDlq = await _dbContext.GradingDeadLetters
                .AsNoTracking()
                .OrderByDescending(d => d.FailedAtUtc)
                .Take(5)
                .Select(d => new RecentDlqItemDto
                {
                    Id = d.Id,
                    StreamMessageId = d.StreamMessageId,
                    SubmissionId = d.SubmissionId,
                    ErrorMessage = d.ErrorMessage,
                    FailedAtUtc = d.FailedAtUtc,
                    IsResolved = d.IsResolved
                })
                .ToListAsync(cancellationToken);

            var streamStatus = unresolvedCount > 0 ? "Warning" : "Healthy";

            return new SystemHealthDto
            {
                UnresolvedDlqCount = unresolvedCount,
                TotalDlqCount = totalDlqCount,
                TotalCertificatesIssued = totalCerts,
                TotalGradeRecords = totalGrades,
                RedisStreamStatus = streamStatus,
                StorageStatus = "Healthy",
                RecentDeadLetters = recentDlq,
                CheckedAtUtc = DateTime.UtcNow
            };
        }, TimeSpan.FromSeconds(10), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
