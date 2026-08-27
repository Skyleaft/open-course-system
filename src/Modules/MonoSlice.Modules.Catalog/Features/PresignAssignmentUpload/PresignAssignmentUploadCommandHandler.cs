using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Catalog.Features.PresignAssignmentUpload;

public sealed class PresignAssignmentUploadCommandHandler : ICommandHandler<PresignAssignmentUploadCommand, ApiResponse<PresignedAssignmentUploadDto>>
{
    private const string BucketName = "assignment-submissions";
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IObjectStorageService _storageService;

    public PresignAssignmentUploadCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IObjectStorageService storageService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _storageService = storageService;
    }

    public async ValueTask<ApiResponse<PresignedAssignmentUploadDto>> Handle(
        PresignAssignmentUploadCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to upload assignment solutions.");
        }

        var assignment = await _dbContext.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            throw new NotFoundException(nameof(Assignment), command.AssignmentId);
        }

        if (DateTime.UtcNow > assignment.DeadlineUtc)
        {
            throw new BusinessRuleException($"Assignment deadline was {assignment.DeadlineUtc:u}. Submissions are no longer accepted.");
        }

        var studentId = _currentUser.UserId.Value;
        var sanitizedFileName = Path.GetFileName(command.FileName);
        var extension = Path.GetExtension(sanitizedFileName);
        var key = $"submissions/{command.AssignmentId}/{studentId}/{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.CreateVersion7():N}{extension}";
        var expiry = TimeSpan.FromMinutes(15);
        var contentType = string.IsNullOrWhiteSpace(command.ContentType) ? "application/octet-stream" : command.ContentType;

        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(
            BucketName,
            key,
            expiry,
            contentType);

        var result = new PresignedAssignmentUploadDto(
            StorageKey: key,
            UploadUrl: uploadUrl,
            ExpiresAtUtc: DateTime.UtcNow.Add(expiry));

        return ApiResponse.Ok(result, "Presigned assignment upload URL generated.");
    }
}
