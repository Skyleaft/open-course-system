using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetProctorLiveSummary;

public sealed class GetProctorLiveSummaryQueryHandler : IQueryHandler<GetProctorLiveSummaryQuery, ApiResponse<ProctorLiveSummaryDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetProctorLiveSummaryQueryHandler(
        ExamsDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<ProctorLiveSummaryDto>> Handle(GetProctorLiveSummaryQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = "cache:dashboard:proctor:live-summary";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;

            var activeSubmissions = await _dbContext.Submissions
                .AsNoTracking()
                .Where(s => s.Status == SubmissionStatus.InProgress && s.MaxAllowedEndTimeUtc > now)
                .ToListAsync(cancellationToken);

            var activeExamIds = activeSubmissions.Select(s => s.ExamId).Distinct().ToList();

            var examLookup = await _dbContext.Exams
                .AsNoTracking()
                .Where(e => activeExamIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Title, cancellationToken);

            var activeExams = activeSubmissions
                .GroupBy(s => s.ExamId)
                .Select(g => new ActiveExamSummaryDto
                {
                    ExamId = g.Key,
                    Title = examLookup.TryGetValue(g.Key, out var title) ? title : "Active Exam",
                    ActiveExaminees = g.Count()
                })
                .ToList();

            var candidateRisks = new List<CandidateLiveRiskDto>();

            foreach (var sub in activeSubmissions)
            {
                var violations = sub.Violations ?? [];
                int tabSwitches = violations.Count(v => v.Type.Contains("Tab", StringComparison.OrdinalIgnoreCase));
                int devTools = violations.Count(v => v.Type.Contains("DevTools", StringComparison.OrdinalIgnoreCase) || v.Type.Contains("Key", StringComparison.OrdinalIgnoreCase));
                int audioSpikes = violations.Count(v => v.Type.Contains("Audio", StringComparison.OrdinalIgnoreCase) || v.Type.Contains("Mic", StringComparison.OrdinalIgnoreCase));
                int faceLost = violations.Count(v => v.Type.Contains("Face", StringComparison.OrdinalIgnoreCase) || v.Type.Contains("Camera", StringComparison.OrdinalIgnoreCase));
                int other = violations.Count - (tabSwitches + devTools + audioSpikes + faceLost);

                double riskScore = (tabSwitches * 2.0) + (devTools * 5.0) + (audioSpikes * 1.5) + (faceLost * 3.0) + (Math.Max(0, other) * 2.0);
                string riskLevel = riskScore >= 10.0 || violations.Count >= 3 ? "High" : riskScore >= 4.0 || violations.Count >= 1 ? "Medium" : "Low";

                if (violations.Count > 0 || riskScore > 0)
                {
                    candidateRisks.Add(new CandidateLiveRiskDto
                    {
                        SubmissionId = sub.Id,
                        ExamId = sub.ExamId,
                        ExamTitle = examLookup.TryGetValue(sub.ExamId, out var eTitle) ? eTitle : "Exam",
                        StudentId = sub.StudentId,
                        ViolationsCount = violations.Count,
                        RiskScore = Math.Round(riskScore, 1),
                        RiskLevel = riskLevel,
                        StartedAtUtc = sub.StartedAtUtc,
                        MaxAllowedEndTimeUtc = sub.MaxAllowedEndTimeUtc
                    });
                }
            }

            var sortedCandidates = candidateRisks
                .OrderByDescending(c => c.RiskScore)
                .ThenByDescending(c => c.ViolationsCount)
                .ToList();

            return new ProctorLiveSummaryDto
            {
                ActiveExamsCount = activeExamIds.Count,
                ActiveExamineesCount = activeSubmissions.Count,
                HighRiskCandidatesCount = sortedCandidates.Count(c => c.RiskLevel == "High"),
                FlaggedCandidates = sortedCandidates,
                ActiveExams = activeExams
            };
        }, TimeSpan.FromSeconds(5), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
