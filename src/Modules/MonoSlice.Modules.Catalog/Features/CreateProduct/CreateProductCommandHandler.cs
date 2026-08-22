using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Catalog.Features.CreateProduct;

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ApiResponse<ProductDto>>
{
    private readonly CatalogDbContext _dbContext;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        CatalogDbContext dbContext,
        IEventBus eventBus,
        ILogger<CreateProductCommandHandler> logger)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<ProductDto>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var skuUpper = command.Sku.Trim().ToUpperInvariant();
        var skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == skuUpper, cancellationToken);
        if (skuExists)
        {
            throw new BusinessRuleException($"Product with SKU '{skuUpper}' already exists.");
        }

        var product = new Product(
            command.Name,
            command.Sku,
            command.Price,
            command.StockQuantity,
            command.Description);

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created product '{ProductName}' with ID {ProductId}", product.Name, product.Id);

        // Publish integration event for cross-module or async event-driven processing (RabbitMQ / Kafka)
        var integrationEvent = new ProductCreatedIntegrationEvent(
            product.Id,
            product.Name,
            product.Sku,
            product.Price,
            product.StockQuantity);

        try
        {
            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ProductCreatedIntegrationEvent to event bus for product {ProductId}", product.Id);
            // In a production system, an outbox pattern ensures at-least-once delivery; here we log the fault
        }

        var dto = product.Adapt<ProductDto>();
        return ApiResponse.Ok(dto, "Product created successfully.");
    }
}
