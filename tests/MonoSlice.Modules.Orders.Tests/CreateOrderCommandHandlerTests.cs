using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Modules.Orders.Services;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Messaging.Events;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public sealed class CreateOrderCommandHandlerTests
{
    private readonly OrdersDbContext _dbContext;
    private readonly ICatalogModuleApi _catalogApi;
    private readonly IUsersModuleApi _usersApi;
    private readonly IEventBus _eventBus;
    private readonly IOrderProcessingQueue _processingQueue;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: $"OrdersDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new OrdersDbContext(options);
        _catalogApi = Substitute.For<ICatalogModuleApi>();
        _usersApi = Substitute.For<IUsersModuleApi>();
        _eventBus = Substitute.For<IEventBus>();
        _processingQueue = Substitute.For<IOrderProcessingQueue>();
        _logger = Substitute.For<ILogger<CreateOrderCommandHandler>>();

        _handler = new CreateOrderCommandHandler(
            _dbContext,
            _catalogApi,
            _usersApi,
            _eventBus,
            _processingQueue,
            _logger);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrder_PublishesIntegrationEvent_And_EnqueuesJob()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();

        // 1. Mock synchronous user module API response
        _usersApi.GetUserByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new UserContractDto(customerId, "john@example.com", "johndoe", ["User"], true));

        // 2. Mock synchronous catalog module API response
        _catalogApi.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductContractDto(productId, "MacBook Pro", "MBP-01", 2000m, 10, true));

        var command = new CreateOrderCommand(
            customerId,
            [new CreateOrderItemDto(productId, 2)],
            "Please deliver quickly",
            autoProcessAsync: true);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(4000m, response.Data.TotalAmount); // 2000 * 2
        Assert.Single(response.Data.Items);

        // Verify order saved to DB
        var savedOrder = await _dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == response.Data.Id);
        Assert.NotNull(savedOrder);
        Assert.Equal(4000m, savedOrder.TotalAmount);

        // Verify asynchronous inter-module event published
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<OrderPlacedIntegrationEvent>(e => e.OrderId == response.Data.Id && e.TotalAmount == 4000m),
            Arg.Any<CancellationToken>());

        // Verify asynchronous in-process queue received order ID
        await _processingQueue.Received(1).EnqueueAsync(
            response.Data.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        _usersApi.GetUserByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns((UserContractDto?)null);

        var command = new CreateOrderCommand(
            customerId,
            [new CreateOrderItemDto(Guid.CreateVersion7(), 1)]);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_CustomerInactive_ThrowsBusinessRuleException()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        _usersApi.GetUserByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new UserContractDto(customerId, "inactive@example.com", "inactive_user", ["User"], false));

        var command = new CreateOrderCommand(
            customerId,
            [new CreateOrderItemDto(Guid.CreateVersion7(), 1)]);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_InsufficientStock_ThrowsBusinessRuleException()
    {
        // Arrange
        var customerId = Guid.CreateVersion7();
        var productId = Guid.CreateVersion7();

        _usersApi.GetUserByIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new UserContractDto(customerId, "john@example.com", "johndoe", ["User"], true));

        _catalogApi.GetProductByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new ProductContractDto(productId, "Rare Item", "RARE-01", 500m, 1, true));

        var command = new CreateOrderCommand(
            customerId,
            [new CreateOrderItemDto(productId, 5)]); // Asking for 5, only 1 available

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(command, CancellationToken.None).AsTask());
    }
}
