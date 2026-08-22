using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Features.ProcessWebhook;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public class ProcessWebhookCommandHandlerTests
{
    private readonly PaymentsDbContext _dbContext;
    private readonly IEventStreamPublisher _eventPublisher;
    private readonly IOptions<PaymentsSettings> _settings;
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;

    public ProcessWebhookCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new PaymentsDbContext(options);
        _eventPublisher = Substitute.For<IEventStreamPublisher>();
        _settings = Options.Create(new PaymentsSettings
        {
            WebhookSecret = "test_webhook_secret"
        });
        _logger = Substitute.For<ILogger<ProcessWebhookCommandHandler>>();
    }

    [Fact]
    public async Task Handle_ShouldMarkOrderAsPaid_AndPublishIntegrationEvent()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 100000m);
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        var handler = new ProcessWebhookCommandHandler(_dbContext, _eventPublisher, _settings, _logger);

        var payload = $"{order.Id}:EXT-PAY-999:PAID";
        var signature = ComputeHmac(payload, _settings.Value.WebhookSecret);

        var command = new ProcessWebhookCommand
        {
            OrderId = order.Id,
            ExternalPaymentReference = "EXT-PAY-999",
            PaymentStatus = "PAID",
            Signature = signature,
            RawPayload = payload
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        var updatedOrder = await _dbContext.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        Assert.Equal("EXT-PAY-999", updatedOrder.ExternalPaymentReference);

        await _eventPublisher.Received(1).PublishAsync(
            "stream:payments-events",
            Arg.Any<OrderPaidIntegrationEvent>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRejectInvalidHmacSignature()
    {
        var order = Order.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 100000m);
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        var handler = new ProcessWebhookCommandHandler(_dbContext, _eventPublisher, _settings, _logger);

        var command = new ProcessWebhookCommand
        {
            OrderId = order.Id,
            ExternalPaymentReference = "EXT-PAY-999",
            PaymentStatus = "PAID",
            Signature = "invalid_signature_hex",
            RawPayload = $"{order.Id}:EXT-PAY-999:PAID"
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None).AsTask());
    }

    private static string ComputeHmac(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
