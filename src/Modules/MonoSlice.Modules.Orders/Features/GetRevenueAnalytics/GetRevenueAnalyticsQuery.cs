using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.GetRevenueAnalytics;

public sealed partial class GetRevenueAnalyticsQuery : IQuery<ApiResponse<RevenueAnalyticsDto>>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public sealed class RevenueAnalyticsDto
{
    public decimal GrossMerchandiseValue { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int TotalOrders { get; init; }
    public int PaidOrders { get; init; }
    public int PendingOrders { get; init; }
    public int FailedOrders { get; init; }
    public int ExpiredOrders { get; init; }
    public double ConversionRate { get; init; }
    public List<DailyRevenuePointDto> DailyTrends { get; init; } = [];
    public List<TopCourseRevenueDto> TopCourses { get; init; } = [];
}

public sealed class DailyRevenuePointDto
{
    public string Date { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}

public sealed class TopCourseRevenueDto
{
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public decimal TotalRevenue { get; init; }
    public int SalesCount { get; init; }
}
