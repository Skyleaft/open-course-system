using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Contracts;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public class PaymentsModuleApiTests
{
    [Fact]
    public async Task HasUserPurchasedCourseAsync_ShouldReturnTrue_WhenOrderIsPaid()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        var dbContext = new PaymentsDbContext(options);
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        var order = Order.Create(userId, courseId, 200000m);
        order.MarkAsPaid("REF-ABC");
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var api = new PaymentsModuleApi(dbContext);

        var purchased = await api.HasUserPurchasedCourseAsync(userId, courseId);
        var notPurchased = await api.HasUserPurchasedCourseAsync(Guid.CreateVersion7(), courseId);

        Assert.True(purchased);
        Assert.False(notPurchased);
    }
}
