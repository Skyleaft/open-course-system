using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.ListOrders;

public sealed record ListOrdersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? CustomerId = null,
    OrderStatus? Status = null) : IQuery<ApiResponse<PaginatedList<OrderDto>>>;
