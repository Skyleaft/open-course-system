namespace MonoSlice.Shared.Abstractions.Messaging;

/// <summary>
/// Abstraction for publishing integration events across modules.
/// Implemented by RabbitMQ or Kafka depending on configuration.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IntegrationEvent;
}
