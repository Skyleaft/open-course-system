namespace MonoSlice.Shared.Abstractions.Messaging;

/// <summary>
/// Handler for integration events received from the message bus.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
