using MonoSlice.Shared.Abstractions.Domain;

namespace MonoSlice.Modules.Catalog.Domain;

public sealed class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    private Product() { } // EF Core

    public Product(string name, string sku, decimal price, int stockQuantity, string? description = null)
        : base(Guid.CreateVersion7())
    {
        SetName(name);
        SetSku(sku);
        SetPrice(price);
        SetStockQuantity(stockQuantity);
        Description = description;
        IsActive = true;

        RaiseDomainEvent(new ProductCreatedEvent(Id, Name, Sku, Price));
    }

    public void UpdateDetails(string name, string? description, decimal price)
    {
        SetName(name);
        SetPrice(price);
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustStock(int quantityChange)
    {
        if (StockQuantity + quantityChange < 0)
        {
            throw new InvalidOperationException($"Insufficient stock for product '{Name}'. Current stock: {StockQuantity}");
        }

        StockQuantity += quantityChange;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    private void SetSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        Sku = sku.Trim().ToUpperInvariant();
    }

    private void SetPrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }
        Price = price;
    }

    private void SetStockQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock quantity cannot be negative.");
        }
        StockQuantity = quantity;
    }
}
