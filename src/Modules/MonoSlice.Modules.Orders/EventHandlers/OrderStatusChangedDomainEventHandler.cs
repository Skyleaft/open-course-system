using Mediator;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Domain.Events;

namespace MonoSlice.Modules.Orders.EventHandlers;

public sealed class OrderStatusChangedDomainEventHandler : INotificationHandler<OrderStatusChangedEvent>
{
    private readonly ILogger<OrderStatusChangedDomainEventHandler> _logger;

    public OrderStatusChangedDomainEventHandler(ILogger<OrderStatusChangedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "In-process Domain Event handled: OrderStatusChangedEvent (OrderId: {OrderId}, {PreviousStatus} -> {NewStatus})",
            notification.OrderId, notification.PreviousStatus, notification.NewStatus);

        return ValueTask.CompletedTask;
    }
}
