using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Orders.Features.GetRevenueAnalytics;

public sealed class GetRevenueAnalyticsQueryHandler : IQueryHandler<GetRevenueAnalyticsQuery, ApiResponse<RevenueAnalyticsDto>>
{
    private readonly PaymentsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ICoursesModuleApi _coursesApi;

    public GetRevenueAnalyticsQueryHandler(
        PaymentsDbContext dbContext,
        ICacheService cacheService,
        ICoursesModuleApi coursesApi)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _coursesApi = coursesApi;
    }

    public async ValueTask<ApiResponse<RevenueAnalyticsDto>> Handle(GetRevenueAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"cache:dashboard:admin:revenue:{query.FromUtc?.ToString("yyyyMMdd") ?? "all"}:{query.ToUtc?.ToString("yyyyMMdd") ?? "all"}";

        var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var from = query.FromUtc ?? DateTime.UtcNow.AddDays(-30);
            var to = query.ToUtc ?? DateTime.UtcNow;

            var orders = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAtUtc >= from && o.CreatedAtUtc <= to)
                .ToListAsync(cancellationToken);

            var totalOrders = orders.Count;
            var paidOrdersList = orders.Where(o => o.Status == OrderStatus.Paid).ToList();
            var paidOrders = paidOrdersList.Count;
            var pendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            var failedOrders = orders.Count(o => o.Status == OrderStatus.Failed);
            var expiredOrders = orders.Count(o => o.Status == OrderStatus.Expired);

            var gmv = paidOrdersList.Sum(o => o.Amount);
            var aov = paidOrders > 0 ? gmv / paidOrders : 0m;
            var conversionRate = totalOrders > 0 ? Math.Round((double)paidOrders / totalOrders * 100, 2) : 0.0;

            // Group daily trends
            var dailyTrends = paidOrdersList
                .GroupBy(o => (o.PaidAtUtc ?? o.CreatedAtUtc).ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => new DailyRevenuePointDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Amount),
                    OrderCount = g.Count()
                })
                .ToList();

            // Top courses by revenue
            var topCourseGroups = paidOrdersList
                .GroupBy(o => o.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    TotalRevenue = g.Sum(x => x.Amount),
                    SalesCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToList();

            var topCourses = new List<TopCourseRevenueDto>();
            foreach (var item in topCourseGroups)
            {
                var courseTitle = "Unknown Course";
                try
                {
                    var course = await _coursesApi.GetCourseByIdAsync(item.CourseId, cancellationToken);
                    if (course is not null)
                    {
                        courseTitle = course.Title;
                    }
                }
                catch
                {
                    // Fallback to default
                }

                topCourses.Add(new TopCourseRevenueDto
                {
                    CourseId = item.CourseId,
                    CourseTitle = courseTitle,
                    TotalRevenue = item.TotalRevenue,
                    SalesCount = item.SalesCount
                });
            }

            return new RevenueAnalyticsDto
            {
                GrossMerchandiseValue = gmv,
                AverageOrderValue = Math.Round(aov, 2),
                TotalOrders = totalOrders,
                PaidOrders = paidOrders,
                PendingOrders = pendingOrders,
                FailedOrders = failedOrders,
                ExpiredOrders = expiredOrders,
                ConversionRate = conversionRate,
                DailyTrends = dailyTrends,
                TopCourses = topCourses
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);

        return ApiResponse.Ok(result);
    }
}
