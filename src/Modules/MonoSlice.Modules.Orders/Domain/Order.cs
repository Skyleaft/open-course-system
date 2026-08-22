using MonoSlice.Modules.Orders.Domain.Events;
using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Orders.Domain;

public sealed class Order : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "IDR";
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? ExternalPaymentReference { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; private set; }

    private Order() : base(Guid.CreateVersion7())
    {
    }

    public static Order Create(
        Guid userId,
        Guid courseId,
        decimal amount,
        string currency = "IDR")
    {
        if (userId == Guid.Empty)
        {
            throw new ValidationException("User ID is required.");
        }

        if (courseId == Guid.Empty)
        {
            throw new ValidationException("Course ID is required.");
        }

        if (amount <= 0)
        {
            throw new BusinessRuleException("Order amount must be greater than zero.");
        }

        return new Order
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CourseId = courseId,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "IDR" : currency.ToUpperInvariant(),
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkAsPaid(string externalPaymentReference)
    {
        if (Status == OrderStatus.Paid)
        {
            // Idempotent operation
            return;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new BusinessRuleException($"Cannot transition order from status '{Status}' to 'Paid'.");
        }

        if (string.IsNullOrWhiteSpace(externalPaymentReference))
        {
            throw new ValidationException("External payment reference cannot be empty.");
        }

        Status = OrderStatus.Paid;
        ExternalPaymentReference = externalPaymentReference;
        PaidAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new OrderPaidDomainEvent(
            Id,
            UserId,
            CourseId,
            Amount,
            PaidAtUtc.Value));
    }

    public void MarkAsExpired()
    {
        if (Status == OrderStatus.Paid)
        {
            throw new BusinessRuleException("Cannot expire an already paid order.");
        }

        if (Status != OrderStatus.Pending)
        {
            return;
        }

        Status = OrderStatus.Expired;
    }

    public void MarkAsFailed()
    {
        if (Status == OrderStatus.Paid)
        {
            throw new BusinessRuleException("Cannot mark a paid order as failed.");
        }

        if (Status != OrderStatus.Pending)
        {
            return;
        }

        Status = OrderStatus.Failed;
    }
}
