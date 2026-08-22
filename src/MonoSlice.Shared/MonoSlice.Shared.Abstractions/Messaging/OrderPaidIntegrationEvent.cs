namespace MonoSlice.Shared.Abstractions.Messaging;

public sealed record OrderPaidIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    Guid CourseId,
    decimal Amount,
    string Currency,
    DateTime PaidAtUtc) : IntegrationEvent;
