using Dapper;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Persistence;

namespace MonoSlice.Modules.Orders.Features.GetOrderAnalytics;

/// <summary>
/// High-performance Dapper query handler for complex multi-table order analytics and aggregations.
/// </summary>
public sealed class GetOrderAnalyticsQueryHandler : IQueryHandler<GetOrderAnalyticsQuery, ApiResponse<OrderAnalyticsDto>>
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public GetOrderAnalyticsQueryHandler(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async ValueTask<ApiResponse<OrderAnalyticsDto>> Handle(
        GetOrderAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            -- 1. Overall Aggregates
            SELECT 
                COUNT(DISTINCT o.id) AS TotalOrders,
                COALESCE(SUM(DISTINCT o.total_amount), 0) AS TotalRevenue,
                COALESCE(AVG(DISTINCT o.total_amount), 0) AS AverageOrderValue,
                COALESCE(SUM(oi.quantity), 0) AS TotalItemsSold
            FROM orders.orders o
            LEFT JOIN orders.order_items oi ON o.id = oi.order_id
            WHERE (@CustomerId IS NULL OR o.customer_id = @CustomerId)
              AND (@FromDate IS NULL OR o.created_at >= @FromDate)
              AND (@ToDate IS NULL OR o.created_at <= @ToDate);

            -- 2. Status Breakdown
            SELECT 
                o.status AS Status,
                COUNT(*) AS Count,
                COALESCE(SUM(o.total_amount), 0) AS TotalAmount
            FROM orders.orders o
            WHERE (@CustomerId IS NULL OR o.customer_id = @CustomerId)
              AND (@FromDate IS NULL OR o.created_at >= @FromDate)
              AND (@ToDate IS NULL OR o.created_at <= @ToDate)
            GROUP BY o.status;

            -- 3. Top 5 Purchased Products
            SELECT 
                oi.product_id AS ProductId,
                oi.product_name AS ProductName,
                COALESCE(SUM(oi.quantity), 0) AS TotalQuantity,
                COALESCE(SUM(oi.total_price), 0) AS TotalRevenue
            FROM orders.order_items oi
            JOIN orders.orders o ON oi.order_id = o.id
            WHERE (@CustomerId IS NULL OR o.customer_id = @CustomerId)
              AND (@FromDate IS NULL OR o.created_at >= @FromDate)
              AND (@ToDate IS NULL OR o.created_at <= @ToDate)
            GROUP BY oi.product_id, oi.product_name
            ORDER BY TotalRevenue DESC
            LIMIT 5;
            """;

        var parameters = new
        {
            CustomerId = query.CustomerId,
            FromDate = query.FromDate,
            ToDate = query.ToDate
        };

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var overview = await multi.ReadFirstOrDefaultAsync<OverviewResult>() ?? new OverviewResult();
        var statusBreakdown = (await multi.ReadAsync<OrderStatusBreakdownDto>()).ToList();
        var topProducts = (await multi.ReadAsync<TopPurchasedProductDto>()).ToList();

        var dto = new OrderAnalyticsDto(
            overview.TotalOrders,
            overview.TotalRevenue,
            overview.AverageOrderValue,
            overview.TotalItemsSold,
            statusBreakdown,
            topProducts);

        return ApiResponse.Ok(dto);
    }

    private sealed class OverviewResult
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
