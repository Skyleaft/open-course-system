using MonoSlice.Modules.Orders.Domain.Events;
using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Orders.Domain;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public Order(Guid customerId, string? notes = null)
        : base(Guid.CreateVersion7())
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));

        CustomerId = customerId;
        Status = OrderStatus.Pending;
        Notes = notes?.Trim();
        TotalAmount = 0;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new BusinessRuleException("Cannot add items to an order that is not in Pending status.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem is not null)
        {
            throw new BusinessRuleException($"Product '{productName}' is already added to this order.");
        }

        var item = new OrderItem(Guid.CreateVersion7(), Id, productId, productName, unitPrice, quantity);
        _items.Add(item);
        RecalculateTotal();
    }

    public void MarkAsPlaced()
    {
        if (_items.Count == 0)
            throw new BusinessRuleException("Cannot place an order with no items.");

        RaiseDomainEvent(new OrderCreatedEvent(Id, CustomerId, TotalAmount));
    }

    public void TransitionToProcessing()
    {
        if (Status != OrderStatus.Pending)
            throw new BusinessRuleException($"Cannot transition order from {Status} to {OrderStatus.Processing}.");

        var prev = Status;
        Status = OrderStatus.Processing;
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, prev, Status));
    }

    public void MarkAsCompleted()
    {
        if (Status != OrderStatus.Processing && Status != OrderStatus.Pending)
            throw new BusinessRuleException($"Cannot complete order with status {Status}.");

        var prev = Status;
        Status = OrderStatus.Completed;
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, prev, Status));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Completed)
            throw new BusinessRuleException("Completed orders cannot be cancelled.");

        if (Status == OrderStatus.Cancelled)
            return;

        var prev = Status;
        Status = OrderStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"Cancelled: {reason}" : $"{Notes} | Cancelled: {reason}";
        RaiseDomainEvent(new OrderStatusChangedEvent(Id, prev, Status));
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.TotalPrice);
    }
}
