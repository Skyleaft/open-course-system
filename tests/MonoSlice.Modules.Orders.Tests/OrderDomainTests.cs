using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Domain.Events;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public class OrderDomainTests
{
    [Fact]
    public void Create_ShouldInitializeWithGuidV7AndPendingStatus()
    {
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();
        var amount = 150000m;

        var order = Order.Create(userId, courseId, amount, "IDR");

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(userId, order.UserId);
        Assert.Equal(courseId, order.CourseId);
        Assert.Equal(amount, order.Amount);
        Assert.Equal("IDR", order.Currency);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.PaidAtUtc);
        Assert.Null(order.ExternalPaymentReference);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenAmountIsZeroOrNegative()
    {
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        Assert.Throws<BusinessRuleException>(() =>
            Order.Create(userId, courseId, 0m));

        Assert.Throws<BusinessRuleException>(() =>
            Order.Create(userId, courseId, -50000m));
    }

    [Fact]
    public void MarkAsPaid_ShouldTransitionToPaid_AndRaiseDomainEvent()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 200000m);
        var extRef = "PAY-12345";

        order.MarkAsPaid(extRef);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(extRef, order.ExternalPaymentReference);
        Assert.NotNull(order.PaidAtUtc);

        var domainEvents = order.DomainEvents;
        Assert.Single(domainEvents);
        var paidEvent = Assert.IsType<OrderPaidDomainEvent>(domainEvents[0]);
        Assert.Equal(order.Id, paidEvent.OrderId);
        Assert.Equal(order.UserId, paidEvent.UserId);
        Assert.Equal(order.CourseId, paidEvent.CourseId);
        Assert.Equal(order.Amount, paidEvent.Amount);
    }

    [Fact]
    public void MarkAsPaid_ShouldBeIdempotent_WhenAlreadyPaid()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 200000m);
        order.MarkAsPaid("REF-1");
        order.ClearDomainEvents();

        // Second call
        order.MarkAsPaid("REF-2");

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("REF-1", order.ExternalPaymentReference);
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void MarkAsExpired_ShouldTransitionToExpired()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 100000m);

        order.MarkAsExpired();

        Assert.Equal(OrderStatus.Expired, order.Status);
    }

    [Fact]
    public void MarkAsFailed_ShouldTransitionToFailed()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 100000m);

        order.MarkAsFailed();

        Assert.Equal(OrderStatus.Failed, order.Status);
    }

    [Fact]
    public void MarkAsPaid_ShouldThrowException_WhenOrderIsFailedOrExpired()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 100000m);
        order.MarkAsFailed();

        Assert.Throws<BusinessRuleException>(() => order.MarkAsPaid("REF-X"));
    }
}
