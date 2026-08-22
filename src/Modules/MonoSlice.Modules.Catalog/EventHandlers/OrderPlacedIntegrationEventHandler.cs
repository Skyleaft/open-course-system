using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;

namespace MonoSlice.Modules.Catalog.EventHandlers;

/// <summary>
/// Handles OrderPlacedIntegrationEvent emitted by Orders module asynchronously over message bus.
/// Adjusts/reserves product stock in the Catalog module.
/// </summary>
public sealed class OrderPlacedIntegrationEventHandler : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    private readonly CatalogDbContext _dbContext;
    private readonly ILogger<OrderPlacedIntegrationEventHandler> _logger;

    public OrderPlacedIntegrationEventHandler(
        CatalogDbContext dbContext,
        ILogger<OrderPlacedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPlacedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Async Worker [Catalog] received OrderPlacedIntegrationEvent: OrderId={OrderId}, ItemsCount={Count}, Total={Total:C}",
            @event.OrderId, @event.Items.Count, @event.TotalAmount);

        foreach (var item in @event.Items)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);
            if (product is not null)
            {
                var originalStock = product.StockQuantity;
                product.AdjustStock(-item.Quantity);
                _logger.LogInformation(
                    "Async Worker [Catalog] reserved stock for Product {ProductId} ({ProductName}): {OldStock} -> {NewStock}",
                    product.Id, product.Name, originalStock, product.StockQuantity);
            }
            else
            {
                _logger.LogWarning("Async Worker [Catalog] could not find Product {ProductId} to adjust stock for Order {OrderId}",
                    item.ProductId, @event.OrderId);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
