using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;

public sealed class GetLiveCandidatesQueryHandler : IQueryHandler<GetLiveCandidatesQuery, ApiResponse<IReadOnlyList<LiveCandidateDto>>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;

    public GetLiveCandidatesQueryHandler(
        ExamsDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<LiveCandidateDto>>> Handle(
        GetLiveCandidatesQuery query,
        CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.ExamId, cancellationToken);

        if (exam is null)
        {
            throw new NotFoundException(nameof(QuizExam), query.ExamId);
        }

        var submissions = await _dbContext.Submissions
            .AsNoTracking()
            .Include(s => s.Snapshots)
            .Where(s => s.ExamId == query.ExamId)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var candidateList = new List<LiveCandidateDto>();

        foreach (var s in submissions)
        {
            var isOnline = false;
            if (s.Status == SubmissionStatus.InProgress)
            {
                var liveness = await _cacheService.GetAsync<bool?>($"exam_liveness:{s.Id}", cancellationToken);
                isOnline = liveness == true;
            }

            var remainingSeconds = Math.Max(0, (long)(s.MaxAllowedEndTimeUtc - DateTime.UtcNow).TotalSeconds);

            candidateList.Add(new LiveCandidateDto(
                s.Id,
                s.StudentId,
                s.Status.ToString(),
                isOnline,
                s.Violations.Count,
                remainingSeconds,
                s.StartedAtUtc,
                s.MaxAllowedEndTimeUtc,
                s.Snapshots.Count));
        }

        return ApiResponse.Ok<IReadOnlyList<LiveCandidateDto>>(candidateList);
    }
}
