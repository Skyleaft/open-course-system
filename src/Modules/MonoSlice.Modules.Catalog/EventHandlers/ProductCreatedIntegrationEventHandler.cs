using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Catalog.EventHandlers;

public sealed class ProductCreatedIntegrationEventHandler : IIntegrationEventHandler<ProductCreatedIntegrationEvent>
{
    private readonly ILogger<ProductCreatedIntegrationEventHandler> _logger;

    public ProductCreatedIntegrationEventHandler(ILogger<ProductCreatedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ProductCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Async Worker handled ProductCreatedIntegrationEvent: ProductId={ProductId}, Name='{ProductName}', Sku='{Sku}', Price={Price}",
            @event.ProductId, @event.Name, @event.Sku, @event.Price);

        return Task.CompletedTask;
    }
}
