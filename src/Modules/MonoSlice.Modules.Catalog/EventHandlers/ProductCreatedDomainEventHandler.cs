using Mediator;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;

namespace MonoSlice.Modules.Catalog.EventHandlers;

public sealed class ProductCreatedDomainEventHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedDomainEventHandler> _logger;

    public ProductCreatedDomainEventHandler(ILogger<ProductCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "In-process Domain Event handled: ProductCreatedEvent (ID: {ProductId}, Name: '{ProductName}', Price: {Price})",
            notification.ProductId, notification.Name, notification.Price);

        return ValueTask.CompletedTask;
    }
}
