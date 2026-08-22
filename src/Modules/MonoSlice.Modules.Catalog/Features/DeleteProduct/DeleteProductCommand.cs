using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Catalog.Features.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand<ApiResponse>;
