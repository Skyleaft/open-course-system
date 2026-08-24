using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.SubmitAssignment;

public sealed class SubmitAssignmentCommandHandler : ICommandHandler<SubmitAssignmentCommand, ApiResponse<SubmissionResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SubmitAssignmentCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<SubmissionResultDto>> Handle(
        SubmitAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to submit assignments.");
        }

        var studentId = _currentUser.UserId.Value;

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

        var existing = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == command.AssignmentId && s.StudentId == studentId, cancellationToken);

        AssignmentSubmission submission;
        if (existing is not null)
        {
            _dbContext.Submissions.Remove(existing);
        }

        submission = AssignmentSubmission.Create(
            command.AssignmentId,
            studentId,
            command.FileUrl,
            assignment.DeadlineUtc);

        await _dbContext.Submissions.AddAsync(submission, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new SubmissionResultDto(
            submission.Id,
            submission.AssignmentId,
            submission.StudentId,
            submission.FileUrl,
            submission.SubmittedAtUtc);

        return ApiResponse.Ok(result, "Assignment submitted successfully.");
    }
}
