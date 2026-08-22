using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Messaging;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class CreateProductCommandHandlerTests
{
    private readonly CatalogDbContext _dbContext;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateProductCommandHandler> _logger;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CatalogDbContext(options);
        _eventBus = Substitute.For<IEventBus>();
        _logger = Substitute.For<ILogger<CreateProductCommandHandler>>();
        _handler = new CreateProductCommandHandler(_dbContext, _eventBus, _logger);
    }

    [Fact]
    public async Task Handle_ShouldSaveProductAndPublishIntegrationEvent()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Monitor 4K",
            Sku = "MON-4K-01",
            Price = 399.99m,
            StockQuantity = 10,
            Description = "Ultra HD 4K Monitor"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Monitor 4K", result.Data.Name);
        Assert.Equal("MON-4K-01", result.Data.Sku);

        var savedProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == result.Data.Id);
        Assert.NotNull(savedProduct);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ProductCreatedIntegrationEvent>(e => e.ProductId == result.Data.Id && e.Sku == "MON-4K-01"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSkuAlreadyExists()
    {
        // Arrange
        var existingProduct = new Product("Existing", "DUPLICATE-SKU", 100m, 5);
        await _dbContext.Products.AddAsync(existingProduct);
        await _dbContext.SaveChangesAsync();

        var command = new CreateProductCommand
        {
            Name = "Duplicate Item",
            Sku = "DUPLICATE-SKU",
            Price = 50m,
            StockQuantity = 2
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _handler.Handle(command, CancellationToken.None).AsTask());
    }
}
