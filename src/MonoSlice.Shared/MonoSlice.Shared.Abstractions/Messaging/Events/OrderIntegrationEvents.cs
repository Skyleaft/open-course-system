namespace MonoSlice.Shared.Abstractions.Messaging.Events;

public sealed record OrderItemContractDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyList<OrderItemContractDto> Items) : IntegrationEvent;

public sealed record OrderCompletedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CompletedAt) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason) : IntegrationEvent;
