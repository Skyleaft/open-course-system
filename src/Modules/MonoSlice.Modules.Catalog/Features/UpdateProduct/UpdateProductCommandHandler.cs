using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.UpdateProduct;

public sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ApiResponse<ProductDto>>
{
    private readonly CatalogDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public UpdateProductCommandHandler(
        CatalogDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<ProductDto>> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", command.Id);
        }

        product.UpdateDetails(command.Name, command.Description, command.Price);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveAsync($"catalog:products:{command.Id}", cancellationToken);

        var dto = product.Adapt<ProductDto>();
        return ApiResponse.Ok(dto, "Product updated successfully.");
    }
}
