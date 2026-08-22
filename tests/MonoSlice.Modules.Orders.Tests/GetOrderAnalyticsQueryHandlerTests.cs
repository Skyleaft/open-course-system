using System.Data;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Features.GetOrderAnalytics;
using MonoSlice.Shared.Abstractions.Persistence;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public sealed class GetOrderAnalyticsQueryHandlerTests
{
    [Fact]
    public void GetOrderAnalyticsQuery_CanBeConstructedWithDefaultAndCustomParameters()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var defaultQuery = new GetOrderAnalyticsQuery();
        var customQuery = new GetOrderAnalyticsQuery(customerId, fromDate, toDate);

        // Assert
        Assert.Null(defaultQuery.CustomerId);
        Assert.Null(defaultQuery.FromDate);
        Assert.Null(defaultQuery.ToDate);

        Assert.Equal(customerId, customQuery.CustomerId);
        Assert.Equal(fromDate, customQuery.FromDate);
        Assert.Equal(toDate, customQuery.ToDate);
    }

    [Fact]
    public void OrderAnalyticsDto_ContainsCorrectStructure()
    {
        // Arrange
        var statusList = new List<OrderStatusBreakdownDto>
        {
            new(OrderStatus.Completed, 10, 500.00m),
            new(OrderStatus.Pending, 2, 100.00m)
        };

        var topProducts = new List<TopPurchasedProductDto>
        {
            new(Guid.NewGuid(), "Mechanical Keyboard", 15, 1500.00m)
        };

        // Act
        var dto = new OrderAnalyticsDto(
            TotalOrders: 12,
            TotalRevenue: 600.00m,
            AverageOrderValue: 50.00m,
            TotalItemsSold: 25,
            StatusBreakdown: statusList,
            TopProducts: topProducts);

        // Assert
        Assert.Equal(12, dto.TotalOrders);
        Assert.Equal(600.00m, dto.TotalRevenue);
        Assert.Equal(50.00m, dto.AverageOrderValue);
        Assert.Equal(25, dto.TotalItemsSold);
        Assert.Equal(2, dto.StatusBreakdown.Count);
        Assert.Single(dto.TopProducts);
    }
}
