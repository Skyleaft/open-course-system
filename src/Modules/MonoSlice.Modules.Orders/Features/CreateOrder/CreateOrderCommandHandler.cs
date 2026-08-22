using Mapster;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Modules.Orders.Services;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;

namespace MonoSlice.Modules.Orders.Features.CreateOrder;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, ApiResponse<OrderDto>>
{
    private readonly OrdersDbContext _dbContext;
    private readonly ICatalogModuleApi _catalogApi;
    private readonly IUsersModuleApi _usersApi;
    private readonly IEventBus _eventBus;
    private readonly IOrderProcessingQueue _processingQueue;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        OrdersDbContext dbContext,
        ICatalogModuleApi catalogApi,
        IUsersModuleApi usersApi,
        IEventBus eventBus,
        IOrderProcessingQueue processingQueue,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _catalogApi = catalogApi;
        _usersApi = usersApi;
        _eventBus = eventBus;
        _processingQueue = processingQueue;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<OrderDto>> Handle(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Synchronous Inter-Module Communication: Verify customer exists in Users module
        _logger.LogInformation("Verifying customer {CustomerId} synchronously with Users module...", command.CustomerId);
        var user = await _usersApi.GetUserByIdAsync(command.CustomerId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User", command.CustomerId);
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleException($"Customer '{user.UserName}' ({user.Email}) is currently inactive.");
        }

        // 2. Build Order aggregate and fetch product details synchronously from Catalog module
        var order = new Order(command.CustomerId, command.Notes);
        var eventItems = new List<OrderItemContractDto>();

        foreach (var itemDto in command.Items)
        {
            _logger.LogInformation("Fetching product {ProductId} details synchronously with Catalog module...", itemDto.ProductId);
            var product = await _catalogApi.GetProductByIdAsync(itemDto.ProductId, cancellationToken);
            if (product is null)
            {
                throw new NotFoundException("Product", itemDto.ProductId);
            }

            if (!product.IsActive)
            {
                throw new BusinessRuleException($"Product '{product.Name}' is no longer active.");
            }

            if (product.StockQuantity < itemDto.Quantity)
            {
                throw new BusinessRuleException(
                    $"Insufficient stock for product '{product.Name}'. Requested: {itemDto.Quantity}, Available: {product.StockQuantity}.");
            }

            order.AddItem(product.Id, product.Name, product.Price, itemDto.Quantity);
            eventItems.Add(new OrderItemContractDto(product.Id, product.Name, product.Price, itemDto.Quantity));
        }

        order.MarkAsPlaced();

        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} placed successfully for customer {CustomerId}. Total: {Total:C}",
            order.Id, order.CustomerId, order.TotalAmount);

        // 3. Asynchronous Inter-Module Communication: Publish OrderPlacedIntegrationEvent to event bus (RabbitMQ/Kafka)
        var integrationEvent = new OrderPlacedIntegrationEvent(
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            eventItems);

        try
        {
            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogInformation("Dispatched OrderPlacedIntegrationEvent to message bus for Order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OrderPlacedIntegrationEvent for order {OrderId}", order.Id);
        }

        // 4. Asynchronous In-Process Processing: Enqueue into background processing channel if requested
        if (command.AutoProcessAsync)
        {
            await _processingQueue.EnqueueAsync(order.Id, cancellationToken);
            _logger.LogInformation("Order {OrderId} enqueued for asynchronous background fulfillment processing.", order.Id);
        }

        var responseDto = order.Adapt<OrderDto>();
        return ApiResponse.Ok(responseDto, "Order created successfully and queued for asynchronous processing.");
    }
}
