using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.ListProducts;

public sealed record ListProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : IQuery<ApiResponse<PaginatedList<ProductDto>>>;
