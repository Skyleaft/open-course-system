using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetCandidateSnapshots;

public sealed class GetCandidateSnapshotsQueryHandler
    : IQueryHandler<GetCandidateSnapshotsQuery, ApiResponse<IReadOnlyList<CandidateSnapshotDto>>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly IObjectStorageService _storageService;

    public GetCandidateSnapshotsQueryHandler(
        ExamsDbContext dbContext,
        IObjectStorageService storageService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<CandidateSnapshotDto>>> Handle(
        GetCandidateSnapshotsQuery query,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .Include(s => s.Snapshots)
            .FirstOrDefaultAsync(s => s.Id == query.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), query.SubmissionId);
        }

        var orderedSnapshots = submission.Snapshots
            .OrderBy(sn => sn.CapturedAtUtc)
            .ToList();

        var result = new List<CandidateSnapshotDto>();

        foreach (var snapshot in orderedSnapshots)
        {
            var presignedUrl = await _storageService.GeneratePresignedDownloadUrlAsync(
                "exam-snapshots",
                snapshot.StorageKey,
                TimeSpan.FromMinutes(30));

            result.Add(new CandidateSnapshotDto(
                snapshot.Id,
                snapshot.SubmissionId,
                snapshot.StorageKey,
                presignedUrl,
                snapshot.CapturedAtUtc));
        }

        return ApiResponse.Ok<IReadOnlyList<CandidateSnapshotDto>>(result);
    }
}
