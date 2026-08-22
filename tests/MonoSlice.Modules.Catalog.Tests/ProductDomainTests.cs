using MonoSlice.Modules.Catalog.Domain;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class ProductDomainTests
{
    [Fact]
    public void Product_Creation_ShouldSetPropertiesAndRaiseDomainEvent()
    {
        // Arrange
        var name = "Mechanical Keyboard";
        var sku = "kb-001";
        var price = 99.99m;
        var stock = 50;
        var description = "Custom mechanical keyboard";

        // Act
        var product = new Product(name, sku, price, stock, description);

        // Assert
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(name, product.Name);
        Assert.Equal("KB-001", product.Sku);
        Assert.Equal(price, product.Price);
        Assert.Equal(stock, product.StockQuantity);
        Assert.True(product.IsActive);
        Assert.Single(product.DomainEvents);
        Assert.IsType<ProductCreatedEvent>(product.DomainEvents[0]);
    }

    [Fact]
    public void AdjustStock_ShouldDecreaseStock_WhenSufficientQuantity()
    {
        // Arrange
        var product = new Product("Mouse", "MS-001", 49.99m, 20);

        // Act
        product.AdjustStock(-5);

        // Assert
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_ShouldThrowException_WhenInsufficientQuantity()
    {
        // Arrange
        var product = new Product("Mouse", "MS-001", 49.99m, 5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.AdjustStock(-10));
    }
}
