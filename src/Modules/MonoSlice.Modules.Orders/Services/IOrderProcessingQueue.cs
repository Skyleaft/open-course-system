namespace MonoSlice.Modules.Orders.Services;

/// <summary>
/// Thread-safe in-process asynchronous queue for background order processing jobs.
/// </summary>
public interface IOrderProcessingQueue
{
    ValueTask EnqueueAsync(Guid orderId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default);
}
