namespace MonoSlice.Modules.Exams.Hubs;

public interface IExamProctorClient
{
    Task CandidateJoined(Guid studentId, Guid submissionId, string connectionId);
    Task ProctorViolationAlert(Guid studentId, Guid submissionId, string violationType, int count, string reason);
    Task ProctorSnapshotReceived(Guid studentId, Guid submissionId, string snapshotPresignedViewUrl, DateTime capturedAtUtc);
    Task CandidateStatusChanged(Guid submissionId, string status);
}
