using Mediator;

namespace MonoSlice.Shared.Abstractions.Domain;

/// <summary>
/// Marker interface for domain events, dispatched via Mediator.
/// </summary>
public interface IDomainEvent : INotification;
