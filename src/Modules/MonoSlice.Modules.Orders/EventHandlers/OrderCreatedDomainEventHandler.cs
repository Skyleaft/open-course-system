using Mediator;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Domain.Events;

namespace MonoSlice.Modules.Orders.EventHandlers;

public sealed class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedDomainEventHandler> _logger;

    public OrderCreatedDomainEventHandler(ILogger<OrderCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "In-process Domain Event handled: OrderCreatedEvent (OrderId: {OrderId}, CustomerId: {CustomerId}, Total: {Total:C})",
            notification.OrderId, notification.CustomerId, notification.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
