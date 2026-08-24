using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Features.CreateCheckout;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Orders.Tests;

public class CreateCheckoutCommandHandlerTests
{
    private readonly PaymentsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<PaymentsSettings> _settings;

    public CreateCheckoutCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new PaymentsDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _settings = Options.Create(new PaymentsSettings
        {
            DefaultCurrency = "IDR",
            PaymentGatewayCheckoutUrl = "https://checkout.gateway.test/pay"
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenCourseIsPaid()
    {
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var coursesApi = Substitute.For<ICoursesModuleApi>();
        coursesApi.GetCourseByIdAsync(courseId, Arg.Any<CancellationToken>())
            .Returns(new CourseContractDto(courseId, "Advanced .NET", "Description", "OpenPaid", 250000m, true));

        _serviceProvider.GetService(typeof(ICoursesModuleApi)).Returns(coursesApi);

        var handler = new CreateCheckoutCommandHandler(_dbContext, _currentUser, _serviceProvider, _settings);

        var command = new CreateCheckoutCommand { CourseId = courseId };
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(userId, result.Data.UserId);
        Assert.Equal(courseId, result.Data.CourseId);
        Assert.Equal(250000m, result.Data.Amount);
        Assert.Contains(result.Data.OrderId.ToString(), result.Data.PaymentUrl);

        var savedOrder = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == result.Data.OrderId);
        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Pending, savedOrder.Status);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenCourseIsNotPaid()
    {
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var coursesApi = Substitute.For<ICoursesModuleApi>();
        coursesApi.GetCourseByIdAsync(courseId, Arg.Any<CancellationToken>())
            .Returns(new CourseContractDto(courseId, "Free Course", "Description", "OpenFree", 0m, true));

        _serviceProvider.GetService(typeof(ICoursesModuleApi)).Returns(coursesApi);

        var handler = new CreateCheckoutCommandHandler(_dbContext, _currentUser, _serviceProvider, _settings);

        var command = new CreateCheckoutCommand { CourseId = courseId };

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None).AsTask());
    }
}
