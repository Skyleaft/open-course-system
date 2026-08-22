using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Orders.Features.GetOrder;

public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, ApiResponse<OrderResponseDto>>
{
    private readonly PaymentsDbContext _dbContext;

    public GetOrderQueryHandler(PaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<OrderResponseDto>> Handle(
        GetOrderQuery query,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException(nameof(Order), query.Id);
        }

        var dto = order.Adapt<OrderResponseDto>();
        return ApiResponse.Ok(dto);
    }
}
