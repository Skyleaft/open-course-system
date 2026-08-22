using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.GetOrder;

public sealed record GetOrderQuery(Guid Id) : IQuery<ApiResponse<OrderDto>>;
