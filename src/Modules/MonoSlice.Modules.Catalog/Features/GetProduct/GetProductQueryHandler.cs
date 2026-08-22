using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.GetProduct;

public sealed class GetProductQueryHandler : IQueryHandler<GetProductQuery, ApiResponse<ProductDto>>
{
    private readonly CatalogDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public GetProductQueryHandler(
        CatalogDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<ProductDto>> Handle(
        GetProductQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"catalog:products:{query.Id}";

        var productDto = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var product = await _dbContext.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

                if (product is null)
                {
                    throw new NotFoundException("Product", query.Id);
                }

                return product.Adapt<ProductDto>();
            },
            TimeSpan.FromMinutes(10),
            cancellationToken);

        return ApiResponse.Ok(productDto);
    }
}
