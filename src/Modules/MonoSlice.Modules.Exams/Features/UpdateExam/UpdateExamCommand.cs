using System.ComponentModel.DataAnnotations;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Features.CreateExam;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.UpdateExam;

public sealed record UpdateExamCommand : ICommand<ApiResponse<ExamDetailDto>>
{
    public Guid Id { get; init; }

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
