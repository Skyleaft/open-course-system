using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Orders.Domain.Events;

public sealed record OrderPaidDomainEvent(
    Guid OrderId,
    Guid UserId,
    Guid CourseId,
    decimal Amount,
    DateTime PaidAtUtc) : IDomainEvent;
