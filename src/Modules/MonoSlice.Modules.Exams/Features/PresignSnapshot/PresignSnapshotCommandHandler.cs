using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Exams.Features.PresignSnapshot;

public sealed class PresignSnapshotCommandHandler : ICommandHandler<PresignSnapshotCommand, ApiResponse<PresignedSnapshotResultDto>>
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IObjectStorageService _storageService;

    public PresignSnapshotCommandHandler(
        ExamsDbContext dbContext,
        ICurrentUser currentUser,
        IObjectStorageService storageService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _storageService = storageService;
    }

    public async ValueTask<ApiResponse<PresignedSnapshotResultDto>> Handle(
        PresignSnapshotCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required.");
        }

        var submission = await _dbContext.Submissions
            .Include(s => s.Snapshots)
            .FirstOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new NotFoundException(nameof(QuizSubmission), command.SubmissionId);
        }

        if (submission.StudentId != _currentUser.UserId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this exam attempt.");
        }

        var key = $"snapshots/{submission.ExamId}/{submission.StudentId}/{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.CreateVersion7():N}.jpg";
        var expiry = TimeSpan.FromMinutes(2);

        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(
            "exam-snapshots",
            key,
            expiry,
            command.ContentType ?? "image/jpeg");

        submission.LogSnapshot(key);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new PresignedSnapshotResultDto(
            key,
            uploadUrl,
            DateTime.UtcNow.Add(expiry));

        return ApiResponse.Ok(result, "Presigned snapshot upload URL generated.");
    }
}
