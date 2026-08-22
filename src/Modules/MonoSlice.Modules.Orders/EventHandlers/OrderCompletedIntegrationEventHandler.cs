using Microsoft.Extensions.Logging;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;

namespace MonoSlice.Modules.Orders.EventHandlers;

/// <summary>
/// Reacts to OrderCompletedIntegrationEvent when published over message bus (e.g. RabbitMQ/Kafka).
/// </summary>
public sealed class OrderCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderCompletedIntegrationEvent>
{
    private readonly ILogger<OrderCompletedIntegrationEventHandler> _logger;

    public OrderCompletedIntegrationEventHandler(ILogger<OrderCompletedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCompletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Async Worker [Orders Consumer] handled OrderCompletedIntegrationEvent: OrderId={OrderId}, CustomerId={CustomerId}, Total={Total:C}, CompletedAt={CompletedAt:u}",
            @event.OrderId, @event.CustomerId, @event.TotalAmount, @event.CompletedAt);

        return Task.CompletedTask;
    }
}
