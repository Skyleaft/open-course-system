using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.GetOrder;

public sealed record GetOrderQuery(Guid Id) : IQuery<ApiResponse<OrderResponseDto>>;

public sealed record OrderResponseDto(
    Guid Id,
    Guid UserId,
    Guid CourseId,
    decimal Amount,
    string Currency,
    string Status,
    string? ExternalPaymentReference,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);
