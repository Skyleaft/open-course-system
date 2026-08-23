using Sannr;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.ProcessWebhook;

public sealed partial class ProcessWebhookCommand : ICommand<ApiResponse<WebhookResponseDto>>
{
    [Required]
    public Guid OrderId { get; init; }

    [Required]
    public string ExternalPaymentReference { get; init; } = string.Empty;

    [Required]
    public string PaymentStatus { get; init; } = string.Empty;

    public string? Signature { get; init; }

    public string? RawPayload { get; init; }
}

public sealed record WebhookResponseDto(
    Guid OrderId,
    string Status,
    bool ProcessedSuccessfully,
    string Message);
