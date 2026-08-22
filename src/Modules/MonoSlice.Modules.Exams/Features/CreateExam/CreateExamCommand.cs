using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.CreateExam;

public sealed record CreateExamCommand : ICommand<ApiResponse<ExamDetailDto>>
{
    public Guid? CourseId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public QuizMode Mode { get; init; } = QuizMode.RealExam;

    public int DurationMinutes { get; init; } = 60;

    public decimal PassingScore { get; init; } = 70m;

    public int MaxAllowedViolations { get; init; } = 3;

    public bool ShuffleQuestions { get; init; } = true;

    public bool ShuffleOptions { get; init; } = true;
}

public sealed record ExamDetailDto(
    Guid Id,
    Guid? CourseId,
    Guid InstructorId,
    string Title,
    string? Description,
    string Mode,
    int DurationMinutes,
    decimal PassingScore,
    int MaxAllowedViolations,
    bool IsPublished,
    bool ShuffleQuestions,
    bool ShuffleOptions,
    DateTime CreatedAtUtc);
