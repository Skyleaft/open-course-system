using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Modules.Orders.Services;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Orders.Features.ProcessOrderAsync;

public sealed class ProcessOrderAsyncCommandHandler : ICommandHandler<ProcessOrderAsyncCommand, ApiResponse<string>>
{
    private readonly OrdersDbContext _dbContext;
    private readonly IOrderProcessingQueue _processingQueue;
    private readonly ILogger<ProcessOrderAsyncCommandHandler> _logger;

    public ProcessOrderAsyncCommandHandler(
        OrdersDbContext dbContext,
        IOrderProcessingQueue processingQueue,
        ILogger<ProcessOrderAsyncCommandHandler> logger)
    {
        _dbContext = dbContext;
        _processingQueue = processingQueue;
        _logger = logger;
    }

    public async ValueTask<ApiResponse<string>> Handle(
        ProcessOrderAsyncCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("Order", command.OrderId);
        }

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessRuleException($"Cannot process order in '{order.Status}' status.");
        }

        await _processingQueue.EnqueueAsync(order.Id, cancellationToken);

        _logger.LogInformation("Order {OrderId} manually enqueued for asynchronous processing worker.", command.OrderId);

        return ApiResponse.Ok($"Order {command.OrderId} successfully enqueued for background processing.", "Order enqueued for processing.");
    }
}
