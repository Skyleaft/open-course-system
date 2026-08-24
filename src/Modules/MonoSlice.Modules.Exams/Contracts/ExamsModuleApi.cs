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
            exam.Mode.ToString(),
            exam.DurationMinutes,
            exam.PassingScore,
            exam.MaxAllowedViolations,
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
            "RealExam",
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
}
