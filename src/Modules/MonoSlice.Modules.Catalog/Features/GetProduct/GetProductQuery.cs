using MonoSlice.Modules.Catalog.Features.CreateProduct;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.GetProduct;

public sealed record GetProductQuery(Guid Id) : IQuery<ApiResponse<ProductDto>>;
