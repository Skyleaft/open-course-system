using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Orders.Contracts;

public sealed class PaymentsModuleApi : IPaymentsModuleApi
{
    private readonly PaymentsDbContext _dbContext;

    public PaymentsModuleApi(PaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderContractDto?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return null;
        }

        return order.Adapt<OrderContractDto>();
    }

    public async Task<bool> IsOrderPaidAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order is not null && order.Status == OrderStatus.Paid;
    }

    public async Task<bool> HasUserPurchasedCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken ct = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .AnyAsync(o => o.UserId == userId && o.CourseId == courseId && o.Status == OrderStatus.Paid, ct);
    }
}
