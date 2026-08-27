namespace MonoSlice.Shared.Abstractions.Contracts;

public record CourseContractDto(
    Guid Id,
    string Title,
    string? Description,
    string AccessType,
    decimal Price,
    bool IsPublished);

public record CourseWithExamsContractDto(
    Guid CourseId,
    string CourseTitle,
    string? CourseDescription,
    string? ThumbnailUrl,
    Guid InstructorId,
    int EnrolledStudentsCount,
    IReadOnlyList<CourseExamContractDto> Exams);

public record CourseExamContractDto(
    Guid ExamId,
    int OrderIndex,
    bool IsMandatory);

public interface ICoursesModuleApi
{
    Task<CourseContractDto?> GetCourseByIdAsync(Guid courseId, CancellationToken ct = default);
    Task<bool> IsStudentEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<bool> EnrollStudentAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<Guid?> GetCourseIdForExamAsync(Guid examId, CancellationToken ct = default);
    Task<IReadOnlyList<CourseWithExamsContractDto>> GetCoursesWithExamsAsync(CancellationToken ct = default);
}
