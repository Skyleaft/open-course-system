using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed record ProductCreatedIntegrationEvent : IntegrationEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }

    public ProductCreatedIntegrationEvent() { }

    public ProductCreatedIntegrationEvent(Guid productId, string name, string sku, decimal price, int stockQuantity)
    {
        ProductId = productId;
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
    }
}
