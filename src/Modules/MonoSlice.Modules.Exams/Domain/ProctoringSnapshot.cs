using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Exams.Domain;

public sealed class ProctoringSnapshot : Entity<Guid>
{
    public Guid SubmissionId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public DateTime CapturedAtUtc { get; private set; } = DateTime.UtcNow;

    private ProctoringSnapshot() : base(Guid.CreateVersion7()) { }

    public static ProctoringSnapshot Create(Guid submissionId, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ValidationException("Snapshot storage key cannot be empty.");
        }

        return new ProctoringSnapshot
        {
            Id = Guid.CreateVersion7(),
            SubmissionId = submissionId,
            StorageKey = storageKey.Trim(),
            CapturedAtUtc = DateTime.UtcNow
        };
    }
}
