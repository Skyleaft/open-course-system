using System.Threading.Channels;

namespace MonoSlice.Modules.Orders.Services;

/// <summary>
/// Channel-backed in-memory asynchronous queue with backpressure handling.
/// </summary>
public sealed class OrderProcessingChannelQueue : IOrderProcessingQueue
{
    private readonly Channel<Guid> _channel;

    public OrderProcessingChannelQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask EnqueueAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(orderId, cancellationToken);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
