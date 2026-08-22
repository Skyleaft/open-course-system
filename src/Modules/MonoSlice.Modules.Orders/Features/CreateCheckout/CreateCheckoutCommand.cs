using System.ComponentModel.DataAnnotations;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.CreateCheckout;

public sealed record CreateCheckoutCommand : ICommand<ApiResponse<CheckoutResponseDto>>
{
    [Required]
    public Guid CourseId { get; init; }

    public decimal? Amount { get; init; }

    public string? Currency { get; init; }

    public string? PaymentMethod { get; init; }
}

public sealed record CheckoutResponseDto(
    Guid OrderId,
    Guid UserId,
    Guid CourseId,
    decimal Amount,
    string Currency,
    string Status,
    string PaymentUrl,
    DateTime CreatedAtUtc);
