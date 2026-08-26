using Sannr;
using MonoSlice.Modules.Exams.Features.CreateExam;
using MonoSlice.Modules.Exams.Features.ExamRules;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Exams.Features.UpdateExam;

public sealed partial class UpdateExamCommand : ICommand<ApiResponse<ExamDetailDto>>
{
    public Guid Id { get; init; }

    [Required]
    [StringLength(255)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? ExamRuleId { get; init; }

    public ExamRuleConfigDto? RuleConfig { get; init; }

    public int DurationMinutes { get; init; } = 60;

    public decimal PassingScore { get; init; } = 70m;

    public int MaxAttempts { get; init; } = 1;

    public DateTime? AvailableFromUtc { get; init; }

    public DateTime? AvailableToUtc { get; init; }

    public bool ShuffleQuestions { get; init; } = true;

    public bool ShuffleOptions { get; init; } = true;

    public List<SectionUpdateDto>? Sections { get; init; }
}

public sealed record SectionUpdateDto(
    Guid? Id,
    Guid QuestionBankId,
    string Title,
    string? Description,
    decimal? PointsOverride,
    int? QuestionCount,
    int OrderIndex);
