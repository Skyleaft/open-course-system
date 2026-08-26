using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;

public sealed class GetLiveCandidatesQueryHandler : IQueryHandler<GetLiveCandidatesQuery, ApiResponse<IReadOnlyList<LiveCandidateDto>>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityModuleApi _identityModuleApi;
    private readonly IObjectStorageService _storageService;

    public GetLiveCandidatesQueryHandler(
        ExamsDbContext dbContext,
        ICacheService cacheService,
        ICurrentUser currentUser,
        IIdentityModuleApi identityModuleApi,
        IObjectStorageService storageService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _currentUser = currentUser;
        _identityModuleApi = identityModuleApi;
        _storageService = storageService;
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

        if (submissions.Count == 0)
        {
            return ApiResponse.Ok<IReadOnlyList<LiveCandidateDto>>(Array.Empty<LiveCandidateDto>());
        }

        var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();
        var users = await _identityModuleApi.GetUsersByIdsAsync(studentIds, cancellationToken);
        var userDict = users.ToDictionary(u => u.Id);

        var candidateList = new List<LiveCandidateDto>();

        foreach (var s in submissions)
        {
            userDict.TryGetValue(s.StudentId, out var user);
            var studentName = user?.FullName;
            if (string.IsNullOrWhiteSpace(studentName))
            {
                studentName = user?.UserName ?? "Student";
            }
            var studentEmail = user?.Email ?? string.Empty;
            var studentAvatarUrl = user?.Picture;

            var isOnline = false;
            if (s.Status == SubmissionStatus.InProgress)
            {
                var liveness = await _cacheService.GetAsync<bool?>($"exam_liveness:{s.Id}", cancellationToken);
                isOnline = liveness == true;
            }

            var remainingSeconds = Math.Max(0, (long)(s.MaxAllowedEndTimeUtc - DateTime.UtcNow).TotalSeconds);

            var latestSnapshot = s.Snapshots.OrderByDescending(sn => sn.CapturedAtUtc).FirstOrDefault();
            string? latestSnapshotUrl = null;
            DateTime? latestSnapshotTime = latestSnapshot?.CapturedAtUtc;

            if (latestSnapshot != null)
            {
                try
                {
                    latestSnapshotUrl = await _storageService.GeneratePresignedDownloadUrlAsync(
                        "exam-snapshots",
                        latestSnapshot.StorageKey,
                        TimeSpan.FromMinutes(30));
                }
                catch
                {
                    // Fallback if storage URL generation encounters transient issue
                    latestSnapshotUrl = null;
                }
            }

            var violations = s.Violations
                .Select(v => new LiveCandidateViolationDto(v.Type, v.Reason, v.TimestampUtc))
                .ToList();

            candidateList.Add(new LiveCandidateDto(
                s.Id,
                s.StudentId,
                studentName,
                studentEmail,
                studentAvatarUrl,
                s.Status.ToString(),
                isOnline,
                s.Violations.Count,
                violations,
                latestSnapshotUrl,
                latestSnapshotTime,
                remainingSeconds,
                s.StartedAtUtc,
                s.MaxAllowedEndTimeUtc,
                s.Snapshots.Count));
        }

        return ApiResponse.Ok<IReadOnlyList<LiveCandidateDto>>(candidateList);
    }
}

