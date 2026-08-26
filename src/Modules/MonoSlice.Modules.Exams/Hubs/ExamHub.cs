using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Storage;

namespace MonoSlice.Modules.Exams.Hubs;

[Authorize]
public sealed class ExamHub : Hub
{
    private readonly ExamsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly IObjectStorageService _storageService;
    private readonly IExamFinalizerService _finalizerService;

    public ExamHub(
        ExamsDbContext dbContext,
        ICacheService cacheService,
        IObjectStorageService storageService,
        IExamFinalizerService finalizerService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _storageService = storageService;
        _finalizerService = finalizerService;
    }

    public async Task JoinExamRoom(Guid submissionId, string sessionToken)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.Id == submissionId);

        if (submission is null)
        {
            throw new HubException("Exam submission not found.");
        }

        // Validate Single Session Token
        var cachedToken = await _cacheService.GetAsync<string>($"exam_session:{submissionId}");
        var isValid = (cachedToken != null && string.Equals(cachedToken, sessionToken, StringComparison.OrdinalIgnoreCase)) ||
                      string.Equals(submission.ActiveSessionToken, sessionToken, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            await Clients.Caller.SendAsync("ForceDisconnectExam", "SessionReplaced");
            return;
        }

        var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => e.Id == submission.ExamId);
        if (exam is null)
        {
            throw new HubException("Exam not found.");
        }

        var examGroup = $"exam_{submissionId}";
        var proctorGroup = $"proctor_exam_{exam.Id}";

        await Groups.AddToGroupAsync(Context.ConnectionId, examGroup);

        // Update student liveness in Redis
        await _cacheService.SetAsync($"exam_liveness:{submissionId}", true, TimeSpan.FromSeconds(30));

        // Notify proctors in proctor room
        var studentId = submission.StudentId;
        await Clients.Group(proctorGroup).SendAsync("CandidateJoined", studentId, submissionId, Context.ConnectionId);

        // Send synchronized timer to caller
        var remainingSeconds = Math.Max(0, (long)(submission.MaxAllowedEndTimeUtc - DateTime.UtcNow).TotalSeconds);
        await Clients.Caller.SendAsync("SyncTimer", remainingSeconds, DateTime.UtcNow);
    }

    public async Task JoinProctorRoom(Guid examId)
    {
        var isAuthorized = Context.User?.IsInRole("Admin") == true ||
                           Context.User?.IsInRole("Instructor") == true ||
                           Context.User?.IsInRole("Proctor") == true;

        if (!isAuthorized)
        {
            throw new HubException("Unauthorized to join proctor monitoring room.");
        }

        var proctorGroup = $"proctor_exam_{examId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, proctorGroup);
    }

    public async Task Heartbeat(Guid submissionId, string sessionToken)
    {
        var submission = await _dbContext.Submissions.FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission is null || submission.Status != SubmissionStatus.InProgress)
        {
            return;
        }

        // Check active session token
        var cachedToken = await _cacheService.GetAsync<string>($"exam_session:{submissionId}");
        if (cachedToken != null && !string.Equals(cachedToken, sessionToken, StringComparison.OrdinalIgnoreCase))
        {
            await Clients.Group($"exam_{submissionId}").SendAsync("ForceDisconnectExam", "SessionReplaced");
            return;
        }

        // Check if timeout reached
        if (DateTime.UtcNow > submission.MaxAllowedEndTimeUtc)
        {
            // Flush buffered Redis answers and finalize submission as TimedOut
            await _finalizerService.FinalizeAndGradeSubmissionAsync(submissionId, SubmissionStatus.TimedOut);

            await Clients.Group($"exam_{submissionId}").SendAsync("ForceDisconnectExam", "Timeout");
            await Clients.Group($"proctor_exam_{submission.ExamId}").SendAsync("CandidateStatusChanged", submissionId, "TimedOut");
            return;
        }

        // Refresh liveness in Redis
        await _cacheService.SetAsync($"exam_liveness:{submissionId}", true, TimeSpan.FromSeconds(30));
    }

    public async Task ReportViolation(Guid submissionId, string violationType, string? details)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.Id == submissionId);

        if (submission is null || submission.Status != SubmissionStatus.InProgress)
        {
            return;
        }

        var exam = await _dbContext.Exams.FirstOrDefaultAsync(e => e.Id == submission.ExamId);
        if (exam is null)
        {
            return;
        }

        var reason = details ?? violationType;
        submission.RecordViolation(violationType, reason);
        await _dbContext.SaveChangesAsync();

        var examGroup = $"exam_{submissionId}";
        var proctorGroup = $"proctor_exam_{exam.Id}";

        // Send warning to student
        await Clients.Group(examGroup).SendAsync(
            "ViolationWarning",
            submission.Violations.Count,
            submission.AppliedRules.MaxAllowedViolations,
            reason);

        // Alert proctors
        await Clients.Group(proctorGroup).SendAsync(
            "ProctorViolationAlert",
            submission.StudentId,
            submission.Id,
            violationType,
            submission.Violations.Count,
            reason);

        // If disqualified, trigger immediate flush and disconnection
        if (submission.Status == SubmissionStatus.Disqualified)
        {
            await _finalizerService.FinalizeAndGradeSubmissionAsync(submissionId, SubmissionStatus.Disqualified, reason);

            await Clients.Group(examGroup).SendAsync("ForceDisconnectExam", "Disqualified");
            await Clients.Group(proctorGroup).SendAsync("CandidateStatusChanged", submissionId, "Disqualified");
        }
    }

    public async Task ReportSnapshotUploaded(Guid submissionId, string objectKey)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.Snapshots)
            .FirstOrDefaultAsync(s => s.Id == submissionId);

        if (submission is null)
        {
            return;
        }

        submission.LogSnapshot(objectKey);
        await _dbContext.SaveChangesAsync();

        var presignedViewUrl = await _storageService.GeneratePresignedDownloadUrlAsync(
            "exam-snapshots",
            objectKey,
            TimeSpan.FromMinutes(10));

        var proctorGroup = $"proctor_exam_{submission.ExamId}";
        await Clients.Group(proctorGroup).SendAsync(
            "ProctorSnapshotReceived",
            submission.StudentId,
            submission.Id,
            presignedViewUrl,
            DateTime.UtcNow);
    }
}
