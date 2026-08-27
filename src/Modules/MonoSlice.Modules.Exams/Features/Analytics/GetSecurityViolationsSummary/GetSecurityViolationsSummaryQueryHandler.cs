using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.Analytics.GetSecurityViolationsSummary;

public sealed class GetSecurityViolationsSummaryQueryHandler : IQueryHandler<GetSecurityViolationsSummaryQuery, ApiResponse<SecurityViolationsSummaryDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetSecurityViolationsSummaryQueryHandler(
        ExamsDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<SecurityViolationsSummaryDto>> Handle(GetSecurityViolationsSummaryQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = "cache:dashboard:admin:security-violations";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var submissions = await _dbContext.Submissions
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var totalSubmissions = submissions.Count;
            var disqualifiedCount = submissions.Count(s => s.Status == SubmissionStatus.Disqualified);
            var disqRate = totalSubmissions > 0 ? Math.Round((double)disqualifiedCount / totalSubmissions * 100, 2) : 0.0;

            // Collect violations
            var allViolations = submissions
                .SelectMany(s => s.Violations ?? Enumerable.Empty<ViolationRecord>())
                .ToList();

            var totalViolations = allViolations.Count;

            var violationTypes = allViolations
                .GroupBy(v => string.IsNullOrWhiteSpace(v.Type) ? "Unknown" : v.Type)
                .Select(g => new ViolationTypeCountDto
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Percentage = totalViolations > 0 ? Math.Round((double)g.Count() / totalViolations * 100, 2) : 0.0
                })
                .OrderByDescending(v => v.Count)
                .ToList();

            // High risk exams
            var examLookup = await _dbContext.Exams
                .AsNoTracking()
                .ToDictionaryAsync(e => e.Id, e => e.Title, cancellationToken);

            var highRiskExams = submissions
                .GroupBy(s => s.ExamId)
                .Select(g => new HighRiskExamDto
                {
                    ExamId = g.Key,
                    ExamTitle = examLookup.TryGetValue(g.Key, out var title) ? title : "Unknown Exam",
                    TotalAttempts = g.Count(),
                    ViolationsCount = g.Sum(s => s.Violations?.Count ?? 0),
                    DisqualifiedCount = g.Count(s => s.Status == SubmissionStatus.Disqualified)
                })
                .OrderByDescending(x => x.ViolationsCount)
                .ThenByDescending(x => x.DisqualifiedCount)
                .Take(5)
                .ToList();

            return new SecurityViolationsSummaryDto
            {
                TotalSubmissions = totalSubmissions,
                TotalViolations = totalViolations,
                DisqualifiedCount = disqualifiedCount,
                DisqualificationRate = disqRate,
                ViolationTypes = violationTypes,
                HighRiskExams = highRiskExams
            };
        }, TimeSpan.FromMinutes(1), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
