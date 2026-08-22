using MonoSlice.Modules.Orders.Services;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public sealed class OrderProcessingQueueTests
{
    [Fact]
    public async Task Enqueue_And_ReadAllAsync_Processes_Items_Asynchronously()
    {
        // Arrange
        var queue = new OrderProcessingChannelQueue(capacity: 10);
        var orderId1 = Guid.CreateVersion7();
        var orderId2 = Guid.CreateVersion7();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await queue.EnqueueAsync(orderId1, cts.Token);
        await queue.EnqueueAsync(orderId2, cts.Token);

        var received = new List<Guid>();
        await foreach (var id in queue.ReadAllAsync(cts.Token))
        {
            received.Add(id);
            if (received.Count == 2)
            {
                break;
            }
        }

        // Assert
        Assert.Equal(2, received.Count);
        Assert.Equal(orderId1, received[0]);
        Assert.Equal(orderId2, received[1]);
    }
}
