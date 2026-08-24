namespace MonoSlice.Shared.Abstractions.Messaging;

/// <summary>
/// Base class for integration events that cross module boundaries via message broker.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}
