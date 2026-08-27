using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Assessments.Features.Admin.GetSystemHealth;

public sealed partial class GetSystemHealthQuery : IQuery<ApiResponse<SystemHealthDto>>
{
}

public sealed class SystemHealthDto
{
    public int UnresolvedDlqCount { get; init; }
    public int TotalDlqCount { get; init; }
    public int TotalCertificatesIssued { get; init; }
    public int TotalGradeRecords { get; init; }
    public string RedisStreamStatus { get; init; } = "Healthy";
    public string StorageStatus { get; init; } = "Healthy";
    public List<RecentDlqItemDto> RecentDeadLetters { get; init; } = [];
    public DateTime CheckedAtUtc { get; init; }
}

public sealed class RecentDlqItemDto
{
    public Guid Id { get; init; }
    public string StreamMessageId { get; init; } = string.Empty;
    public Guid SubmissionId { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public DateTime FailedAtUtc { get; init; }
    public bool IsResolved { get; init; }
}
