namespace MonoSlice.Modules.Exams.Hubs;

public interface IExamClient
{
    Task SyncTimer(long remainingSeconds, DateTime serverTimeUtc);
    Task ViolationWarning(int currentViolationCount, int maxAllowedViolations, string reason);
    Task ForceDisconnectExam(string terminationReason);
    Task ProctorMessage(string message);
}
