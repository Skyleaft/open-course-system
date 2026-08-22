using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;

namespace MonoSlice.Modules.Orders.Features.CancelOrder;

public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, ApiResponse<string>>
{
    private readonly OrdersDbContext _dbContext;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        OrdersDbContext dbContext,
        IEventBus eventBus,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<string>> Handle(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("Order", command.OrderId);
        }

        order.Cancel(command.Reason ?? "Cancelled by user");
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} was cancelled.", order.Id);

        // Publish OrderCancelledIntegrationEvent to event bus
        try
        {
            var cancelledEvent = new OrderCancelledIntegrationEvent(
                order.Id,
                order.CustomerId,
                command.Reason ?? "Cancelled by user");

            await _eventBus.PublishAsync(cancelledEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OrderCancelledIntegrationEvent for order {OrderId}", order.Id);
        }

        return ApiResponse.Ok($"Order {command.OrderId} has been cancelled successfully.", "Order cancelled successfully.");
    }
}
