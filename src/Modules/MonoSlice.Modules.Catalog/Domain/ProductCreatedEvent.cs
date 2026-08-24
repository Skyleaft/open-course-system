using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price) : IDomainEvent;
