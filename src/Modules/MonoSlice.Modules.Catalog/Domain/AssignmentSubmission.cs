using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class AssignmentSubmission : AggregateRoot<Guid>
{
    public Guid AssignmentId { get; private set; }
    public Guid StudentId { get; private set; }
    public string FileUrl { get; private set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; private set; } = DateTime.UtcNow;
    public decimal? Score { get; private set; }
    public string? Feedback { get; private set; }
    public DateTime? GradedAtUtc { get; private set; }

    private AssignmentSubmission() : base(Guid.CreateVersion7()) { }

    public static AssignmentSubmission Create(
        Guid assignmentId,
        Guid studentId,
        string fileUrl,
        DateTime deadlineUtc)
    {
        if (assignmentId == Guid.Empty)
        {
            throw new ValidationException("Assignment ID is required.");
        }

        if (studentId == Guid.Empty)
        {
            throw new ValidationException("Student ID is required.");
        }

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            throw new ValidationException("Submission file URL/storage key cannot be empty.");
        }

        if (DateTime.UtcNow > deadlineUtc)
        {
            throw new BusinessRuleException("Cannot submit assignment after deadline.");
        }

        return new AssignmentSubmission
        {
            Id = Guid.CreateVersion7(),
            AssignmentId = assignmentId,
            StudentId = studentId,
            FileUrl = fileUrl.Trim(),
            SubmittedAtUtc = DateTime.UtcNow
        };
    }

    public void Grade(decimal score, string? feedback)
    {
        if (score < 0)
        {
            throw new ValidationException("Score cannot be negative.");
        }

        Score = score;
        Feedback = feedback?.Trim();
        GradedAtUtc = DateTime.UtcNow;
    }
}
