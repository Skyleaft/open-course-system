using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.CreateAssignment;

public sealed record CreateAssignmentCommand : ICommand<ApiResponse<AssignmentResultDto>>
{
    public Guid CourseId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Instruction { get; init; } = string.Empty;

    public DateTime DeadlineUtc { get; init; }

    public decimal MaxScore { get; init; } = 100m;
}

public sealed record AssignmentResultDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Instruction,
    DateTime DeadlineUtc,
    decimal MaxScore);
