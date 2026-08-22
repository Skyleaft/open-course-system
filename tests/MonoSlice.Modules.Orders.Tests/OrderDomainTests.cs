using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Domain.Events;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public sealed class OrderDomainTests
{
    [Fact]
    public void Order_Creation_Sets_Pending_Status_And_Zero_Total()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();

        // Act
        var order = new Order(customerId, "Test note");

        // Assert
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0, order.TotalAmount);
        Assert.Equal("Test note", order.Notes);
        Assert.Empty(order.Items);
    }

    [Fact]
    public void AddItem_Calculates_Total_Correctly()
    {
        // Arrange
        var order = new Order(Guid.CreateVersion7());
        var prod1 = Guid.CreateVersion7();
        var prod2 = Guid.CreateVersion7();

        // Act
        order.AddItem(prod1, "Laptop", 1000m, 2);
        order.AddItem(prod2, "Mouse", 50m, 3);

        // Assert
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(2150m, order.TotalAmount); // (1000 * 2) + (50 * 3) = 2150
    }

    [Fact]
    public void MarkAsPlaced_Raises_OrderCreatedEvent()
    {
        // Arrange
        var order = new Order(Guid.CreateVersion7());
        order.AddItem(Guid.CreateVersion7(), "Keyboard", 100m, 1);

        // Act
        order.MarkAsPlaced();

        // Assert
        var domainEvent = Assert.Single(order.DomainEvents);
        var orderCreated = Assert.IsType<OrderCreatedEvent>(domainEvent);
        Assert.Equal(order.Id, orderCreated.OrderId);
        Assert.Equal(100m, orderCreated.TotalAmount);
    }

    [Fact]
    public void Order_Status_Transitions_Correctly()
    {
        // Arrange
        var order = new Order(Guid.CreateVersion7());
        order.AddItem(Guid.CreateVersion7(), "Monitor", 300m, 1);

        // Act & Assert Transition to Processing
        order.TransitionToProcessing();
        Assert.Equal(OrderStatus.Processing, order.Status);

        // Act & Assert Transition to Completed
        order.MarkAsCompleted();
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Cancel_Pending_Order_Updates_Status_To_Cancelled()
    {
        // Arrange
        var order = new Order(Guid.CreateVersion7());
        order.AddItem(Guid.CreateVersion7(), "Desk", 250m, 1);

        // Act
        order.Cancel("Customer requested cancellation");

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Contains("Cancelled: Customer requested cancellation", order.Notes);
    }

    [Fact]
    public void Cannot_Cancel_Completed_Order()
    {
        // Arrange
        var order = new Order(Guid.CreateVersion7());
        order.AddItem(Guid.CreateVersion7(), "Chair", 150m, 1);
        order.TransitionToProcessing();
        order.MarkAsCompleted();

        // Act & Assert
        Assert.Throws<BusinessRuleException>(() => order.Cancel("Too late"));
    }
}
