using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class Course : AggregateRoot<Guid>
{
    public Guid InstructorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public CourseAccessType AccessType { get; private set; } = CourseAccessType.OpenFree;
    public decimal Price { get; private set; }
    public string? EnrollmentKeyHash { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<CourseSection> _sections = [];
    public IReadOnlyList<CourseSection> Sections => _sections.AsReadOnly();

    private readonly List<Assignment> _assignments = [];
    public IReadOnlyList<Assignment> Assignments => _assignments.AsReadOnly();

    private readonly List<CourseExam> _exams = [];
    public IReadOnlyList<CourseExam> Exams => _exams.AsReadOnly();

    private Course() : base(Guid.CreateVersion7()) { }

    public static Course Create(
        Guid instructorId,
        string title,
        string? description,
        CourseAccessType accessType,
        decimal price = 0m,
        string? enrollmentKeyHash = null,
        string? thumbnailUrl = null)
    {
        if (instructorId == Guid.Empty)
        {
            throw new ValidationException("Instructor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Course title cannot be empty.");
        }

        if (accessType == CourseAccessType.OpenPaid && price <= 0)
        {
            throw new BusinessRuleException("Price must be greater than zero for OpenPaid courses.");
        }

        if (accessType == CourseAccessType.PrivateWithKey && string.IsNullOrWhiteSpace(enrollmentKeyHash))
        {
            throw new BusinessRuleException("Enrollment key hash is required for PrivateWithKey courses.");
        }

        return new Course
        {
            Id = Guid.CreateVersion7(),
            InstructorId = instructorId,
            Title = title.Trim(),
            Description = description?.Trim(),
            AccessType = accessType,
            Price = accessType == CourseAccessType.OpenPaid ? price : 0m,
            EnrollmentKeyHash = accessType == CourseAccessType.PrivateWithKey ? enrollmentKeyHash : null,
            ThumbnailUrl = thumbnailUrl?.Trim(),
            IsPublished = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string? description,
        CourseAccessType accessType,
        decimal price,
        string? enrollmentKeyHash,
        string? thumbnailUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Course title cannot be empty.");
        }

        if (accessType == CourseAccessType.OpenPaid && price <= 0)
        {
            throw new BusinessRuleException("Price must be greater than zero for OpenPaid courses.");
        }

        Title = title.Trim();
        Description = description?.Trim();
        AccessType = accessType;
        Price = accessType == CourseAccessType.OpenPaid ? price : 0m;
        EnrollmentKeyHash = accessType == CourseAccessType.PrivateWithKey ? enrollmentKeyHash : null;
        ThumbnailUrl = thumbnailUrl?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish()
    {
        IsPublished = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public CourseSection AddSection(string title)
    {
        var section = CourseSection.Create(Id, title, _sections.Count + 1);
        _sections.Add(section);
        UpdatedAtUtc = DateTime.UtcNow;
        return section;
    }

    public Assignment AddAssignment(string title, string instruction, DateTime deadlineUtc, decimal maxScore = 100m)
    {
        var assignment = Assignment.Create(Id, title, instruction, deadlineUtc, maxScore);
        _assignments.Add(assignment);
        UpdatedAtUtc = DateTime.UtcNow;
        return assignment;
    }

    public CourseExam AttachExam(Guid examId, bool isMandatory = true)
    {
        if (examId == Guid.Empty)
        {
            throw new ValidationException("Exam ID is required.");
        }

        var existing = _exams.FirstOrDefault(e => e.ExamId == examId);
        if (existing is not null)
        {
            throw new BusinessRuleException("This exam is already attached to this course.");
        }

        var courseExam = CourseExam.Create(Id, examId, _exams.Count + 1, isMandatory);
        _exams.Add(courseExam);
        UpdatedAtUtc = DateTime.UtcNow;
        return courseExam;
    }

    public void DetachExam(Guid examId)
    {
        var courseExam = _exams.FirstOrDefault(e => e.ExamId == examId || e.Id == examId);
        if (courseExam is not null)
        {
            _exams.Remove(courseExam);
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
