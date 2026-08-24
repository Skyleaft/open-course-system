using Sannr;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.CreateExam;

public sealed partial class CreateExamCommand : ICommand<ApiResponse<ExamDetailDto>>
{
    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public QuizMode Mode { get; init; } = QuizMode.RealExam;

    public int DurationMinutes { get; init; } = 60;

    public decimal PassingScore { get; init; } = 70m;

    public int MaxAllowedViolations { get; init; } = 3;

    public int MaxAttempts { get; init; } = 1;

    public DateTime? AvailableFromUtc { get; init; }

    public DateTime? AvailableToUtc { get; init; }

    public bool ShuffleQuestions { get; init; } = true;

    public bool ShuffleOptions { get; init; } = true;
}

public sealed record ExamDetailDto(
    Guid Id,
    Guid InstructorId,
    string Title,
    string? Description,
    string Mode,
    int DurationMinutes,
    decimal PassingScore,
    int MaxAllowedViolations,
    int MaxAttempts,
    DateTime? AvailableFromUtc,
    DateTime? AvailableToUtc,
    bool IsPublished,
    bool ShuffleQuestions,
    bool ShuffleOptions,
    Guid CreatedBy,
    Guid? UpdatedBy,
    DateTime CreatedAtUtc);
