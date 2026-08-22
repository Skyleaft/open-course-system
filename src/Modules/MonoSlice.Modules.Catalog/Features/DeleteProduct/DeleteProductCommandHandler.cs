using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.DeleteProduct;

public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, ApiResponse>
{
    private readonly CatalogDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public DeleteProductCommandHandler(
        CatalogDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", command.Id);
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"catalog:products:{command.Id}", cancellationToken);

        return ApiResponse.Ok($"Product '{product.Name}' deleted successfully.");
    }
}
