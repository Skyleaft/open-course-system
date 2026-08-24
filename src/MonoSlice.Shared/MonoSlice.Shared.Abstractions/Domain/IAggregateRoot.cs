namespace MonoSlice.Shared.Abstractions.Domain;

/// <summary>
/// Non-generic aggregate root interface for reflection-free event extraction.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
