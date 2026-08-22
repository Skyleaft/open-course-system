using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;

namespace MonoSlice.Modules.Orders.Services;

/// <summary>
/// Long-running background worker that consumes orders from the async queue,
/// processes fulfillment / payment asynchronously, and emits integration events upon completion.
/// </summary>
public sealed class OrderProcessingBackgroundService : BackgroundService
{
    private readonly IOrderProcessingQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderProcessingBackgroundService> _logger;

    public OrderProcessingBackgroundService(
        IOrderProcessingQueue queue,
        IServiceProvider serviceProvider,
        ILogger<OrderProcessingBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderProcessingBackgroundService started and listening for asynchronous processing jobs.");

        try
        {
            await foreach (var orderId in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessOrderAsync(orderId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("OrderProcessingBackgroundService is stopping gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in OrderProcessingBackgroundService execution loop.");
        }
    }

    private async Task ProcessOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Async Worker [Orders] starting background processing for Order {OrderId}", orderId);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        try
        {
            var order = await dbContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Async Worker [Orders] Order {OrderId} not found.", orderId);
                return;
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                _logger.LogInformation("Async Worker [Orders] Order {OrderId} already in terminal status {Status}.", orderId, order.Status);
                return;
            }

            // Step 1: Transition to Processing
            if (order.Status == OrderStatus.Pending)
            {
                order.TransitionToProcessing();
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Async Worker [Orders] Order {OrderId} status transitioned to {Status}", orderId, order.Status);
            }

            // Step 2: Simulate asynchronous external task (e.g. payment gateway transaction, fraud checks, invoice generation)
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

            // Step 3: Complete order
            order.MarkAsCompleted();
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Async Worker [Orders] successfully completed processing for Order {OrderId}. Total: {Total:C}",
                order.Id, order.TotalAmount);

            // Step 4: Dispatch cross-module integration event over message broker
            var completedEvent = new OrderCompletedIntegrationEvent(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                DateTime.UtcNow);

            await eventBus.PublishAsync(completedEvent, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Async Worker [Orders] failed processing Order {OrderId}", orderId);
        }
    }
}
