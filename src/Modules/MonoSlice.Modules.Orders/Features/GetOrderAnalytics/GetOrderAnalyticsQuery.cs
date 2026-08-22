using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.GetOrderAnalytics;

/// <summary>
/// Query for retrieving complex order analytics, status breakdown, and top products.
/// </summary>
public sealed record GetOrderAnalyticsQuery(
    Guid? CustomerId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<ApiResponse<OrderAnalyticsDto>>;

public sealed record OrderAnalyticsDto(
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int TotalItemsSold,
    IReadOnlyList<OrderStatusBreakdownDto> StatusBreakdown,
    IReadOnlyList<TopPurchasedProductDto> TopProducts);

public sealed record OrderStatusBreakdownDto(
    OrderStatus Status,
    int Count,
    decimal TotalAmount);

public sealed record TopPurchasedProductDto(
    Guid ProductId,
    string ProductName,
    int TotalQuantity,
    decimal TotalRevenue);
