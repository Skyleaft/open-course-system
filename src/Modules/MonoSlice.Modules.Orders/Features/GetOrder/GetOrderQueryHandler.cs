using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Orders.Features.GetOrder;

public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, ApiResponse<OrderDto>>
{
    private readonly OrdersDbContext _dbContext;

    public GetOrderQueryHandler(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<OrderDto>> Handle(
        GetOrderQuery query,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("Order", query.Id);
        }

        var dto = order.Adapt<OrderDto>();
        return ApiResponse.Ok(dto);
    }
}
