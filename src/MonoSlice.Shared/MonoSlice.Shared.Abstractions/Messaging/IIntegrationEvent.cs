namespace MonoSlice.Shared.Abstractions.Messaging;

/// <summary>
/// Contract for integration events that cross bounded context boundaries.
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
