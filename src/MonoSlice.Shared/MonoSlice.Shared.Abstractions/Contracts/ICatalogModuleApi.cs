namespace MonoSlice.Shared.Abstractions.Contracts;

public interface ICatalogModuleApi
{
    Task<ProductContractDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> HasSufficientStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}

public sealed record ProductContractDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity,
    bool IsActive);
