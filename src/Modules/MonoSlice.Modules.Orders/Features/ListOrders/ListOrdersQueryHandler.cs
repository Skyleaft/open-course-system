using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Features.CreateOrder;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Orders.Features.ListOrders;

public sealed class ListOrdersQueryHandler : IQueryHandler<ListOrdersQuery, ApiResponse<PaginatedList<OrderDto>>>
{
    private readonly OrdersDbContext _dbContext;

    public ListOrdersQueryHandler(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<PaginatedList<OrderDto>>> Handle(
        ListOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var queryable = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsQueryable();

        if (query.CustomerId.HasValue)
        {
            queryable = queryable.Where(o => o.CustomerId == query.CustomerId.Value);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(o => o.Status == query.Status.Value);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var orders = await queryable
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Adapt<List<OrderDto>>();

        var paginated = new PaginatedList<OrderDto>(dtos, totalCount, query.PageNumber, query.PageSize);
        return ApiResponse.Ok(paginated);
    }
}
