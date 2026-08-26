using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Contracts;

public sealed class ExamsModuleApi : IExamsModuleApi
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public ExamsModuleApi(
        ExamsDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<QuizExamContractDto?> GetExamByIdAsync(Guid quizId, CancellationToken ct = default)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == quizId, ct);

        if (exam is null)
        {
            return null;
        }

        return new QuizExamContractDto(
            exam.Id,
            exam.Title,
            exam.RuleConfig.Name,
            exam.DurationMinutes,
            exam.PassingScore,
            exam.RuleConfig.MaxAllowedViolations,
            exam.IsPublished);
    }

    public async Task<QuizSubmissionContractDto?> GetSubmissionByIdAsync(Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

        if (submission is null)
        {
            return null;
        }

        Guid? parsedToken = Guid.TryParse(submission.ActiveSessionToken, out var guidToken) ? guidToken : null;

        return new QuizSubmissionContractDto(
            submission.Id,
            submission.ExamId,
            submission.StudentId,
            submission.AppliedRules.Name,
            submission.StartedAtUtc,
            submission.MaxAllowedEndTimeUtc,
            submission.SubmittedAtUtc,
            submission.Status.ToString(),
            submission.Score ?? 0m,
            parsedToken);
    }

    public async Task<bool> ValidateActiveSessionAsync(Guid submissionId, Guid sessionToken, CancellationToken ct = default)
    {
        var cachedToken = await _cacheService.GetAsync<string>($"exam_session:{submissionId}", ct);
        if (cachedToken is null)
        {
            return false;
        }

        return string.Equals(cachedToken, sessionToken.ToString("N"), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(cachedToken, sessionToken.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<QuizSubmissionContractDto>> GetStudentSubmissionsForExamsAsync(
        Guid studentId,
        IEnumerable<Guid> examIds,
        CancellationToken ct = default)
    {
        var examIdList = examIds.ToList();
        if (examIdList.Count == 0) return [];

        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && examIdList.Contains(s.ExamId))
            .ToListAsync(ct);

        return submissions.Select(s =>
        {
            Guid? parsedToken = Guid.TryParse(s.ActiveSessionToken, out var guidToken) ? guidToken : null;
            return new QuizSubmissionContractDto(
                s.Id,
                s.ExamId,
                s.StudentId,
                s.AppliedRules.Name,
                s.StartedAtUtc,
                s.MaxAllowedEndTimeUtc,
                s.SubmittedAtUtc,
                s.Status.ToString(),
                s.Score ?? 0m,
                parsedToken);
        }).ToList();
    }

    public async Task<IReadOnlyList<QuizSubmissionContractDto>> GetStudentsSubmissionsForExamsAsync(
        IEnumerable<Guid> studentIds,
        IEnumerable<Guid> examIds,
        CancellationToken ct = default)
    {
        var studentIdList = studentIds.ToList();
        var examIdList = examIds.ToList();
        if (studentIdList.Count == 0 || examIdList.Count == 0) return [];

        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => studentIdList.Contains(s.StudentId) && examIdList.Contains(s.ExamId))
            .ToListAsync(ct);

        return submissions.Select(s =>
        {
            Guid? parsedToken = Guid.TryParse(s.ActiveSessionToken, out var guidToken) ? guidToken : null;
            return new QuizSubmissionContractDto(
                s.Id,
                s.ExamId,
                s.StudentId,
                s.AppliedRules.Name,
                s.StartedAtUtc,
                s.MaxAllowedEndTimeUtc,
                s.SubmittedAtUtc,
                s.Status.ToString(),
                s.Score ?? 0m,
                parsedToken);
        }).ToList();
    }
}
