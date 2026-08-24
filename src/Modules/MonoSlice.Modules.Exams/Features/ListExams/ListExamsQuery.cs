using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using Sannr;

namespace MonoSlice.Modules.Exams.Features.ListExams;

public sealed partial class ListExamsQuery : IQuery<ApiResponse<PaginatedList<ExamSummaryDto>>>
{
    public QuizMode? Mode { get; init; } = null;
    public bool? IsPublished { get; init; } = null;
    public string? SearchTerm { get; init; } = null;
    [Range(1, int.MaxValue)]
    public int PageIndex { get; init; } = 1;
    [Range(1, 1000)]
    public int PageSize { get; init; } = 20;

    public ListExamsQuery() { }

    public ListExamsQuery(
        QuizMode? mode = null,
        bool? isPublished = null,
        string? searchTerm = null,
        int pageIndex = 1,
        int pageSize = 20)
    {
        Mode = mode;
        IsPublished = isPublished;
        SearchTerm = searchTerm;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}

public sealed record ExamSummaryDto(
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
    int SectionsCount,
    int QuestionsCount,
    Guid CreatedBy,
    Guid? UpdatedBy,
    DateTime CreatedAtUtc);
