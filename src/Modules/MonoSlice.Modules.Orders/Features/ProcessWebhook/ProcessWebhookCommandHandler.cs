using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Orders.Features.ProcessWebhook;

public sealed class ProcessWebhookCommandHandler : ICommandHandler<ProcessWebhookCommand, ApiResponse<WebhookResponseDto>>
{
    private readonly PaymentsDbContext _dbContext;
    private readonly IEventStreamPublisher _eventPublisher;
    private readonly PaymentsSettings _settings;
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;

    public ProcessWebhookCommandHandler(
        PaymentsDbContext dbContext,
        IEventStreamPublisher eventPublisher,
        IOptions<PaymentsSettings> settings,
        ILogger<ProcessWebhookCommandHandler> logger)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<WebhookResponseDto>> Handle(
        ProcessWebhookCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Verify HMAC Signature if provided and secret is configured
        if (!string.IsNullOrWhiteSpace(command.Signature) && !string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            var payloadToVerify = command.RawPayload ?? $"{command.OrderId}:{command.ExternalPaymentReference}:{command.PaymentStatus}";
            if (!ValidateHmacSha256(payloadToVerify, _settings.WebhookSecret, command.Signature))
            {
                _logger.LogWarning("Webhook signature verification failed for Order {OrderId}", command.OrderId);
                throw new ValidationException("Invalid webhook HMAC signature.");
            }
        }

        // 2. Fetch Order
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException(nameof(Order), command.OrderId);
        }

        // 3. Idempotency Check
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} is already marked as Paid. Idempotent webhook response returned.", order.Id);
            return ApiResponse.Ok(
                new WebhookResponseDto(order.Id, order.Status.ToString(), true, "Order is already paid."),
                "Webhook processed idempotently.");
        }

        // 4. Process Status
        var normalizedStatus = command.PaymentStatus.ToUpperInvariant();
        if (normalizedStatus is "PAID" or "SUCCESS" or "COMPLETED" or "SETTLEMENT")
        {
            order.MarkAsPaid(command.ExternalPaymentReference);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Publish integration event to Redis Stream / Event Stream
            var integrationEvent = new OrderPaidIntegrationEvent(
                order.Id,
                order.UserId,
                order.CourseId,
                order.Amount,
                order.Currency,
                order.PaidAtUtc ?? DateTime.UtcNow);

            await _eventPublisher.PublishAsync(
                "stream:payments-events",
                integrationEvent,
                ct: cancellationToken);

            _logger.LogInformation("Order {OrderId} successfully marked as Paid via webhook.", order.Id);
        }
        else if (normalizedStatus is "EXPIRED")
        {
            order.MarkAsExpired();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (normalizedStatus is "FAILED" or "DENIED" or "CANCEL")
        {
            order.MarkAsFailed();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var responseDto = new WebhookResponseDto(
            order.Id,
            order.Status.ToString(),
            true,
            $"Order status transitioned to '{order.Status}'.");

        return ApiResponse.Ok(responseDto, "Webhook processed successfully.");
    }

    private static bool ValidateHmacSha256(string payload, string secret, string expectedSignature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedHex = Convert.ToHexString(hash).ToLowerInvariant();
        var expectedHex = expectedSignature.Trim().ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(expectedHex));
    }
}
