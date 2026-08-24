using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MonoSlice.Modules.Orders.Domain;
using MonoSlice.Modules.Orders.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Orders.Features.CreateCheckout;

public sealed class CreateCheckoutCommandHandler : ICommandHandler<CreateCheckoutCommand, ApiResponse<CheckoutResponseDto>>
{
    private readonly PaymentsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceProvider _serviceProvider;
    private readonly PaymentsSettings _settings;

    public CreateCheckoutCommandHandler(
        PaymentsDbContext dbContext,
        ICurrentUser currentUser,
        IServiceProvider serviceProvider,
        IOptions<PaymentsSettings> settings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public async ValueTask<ApiResponse<CheckoutResponseDto>> Handle(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to initiate checkout.");
        }

        var userId = _currentUser.UserId.Value;
        var amount = command.Amount ?? 0;
        var currency = !string.IsNullOrWhiteSpace(command.Currency) ? command.Currency : _settings.DefaultCurrency;

        // Verify with Courses module if available
        var coursesApi = _serviceProvider.GetService<ICoursesModuleApi>();
        if (coursesApi is not null)
        {
            var course = await coursesApi.GetCourseByIdAsync(command.CourseId, cancellationToken);
            if (course is not null)
            {
                if (!string.Equals(course.AccessType, "OpenPaid", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException($"Course '{course.Title}' does not require purchase (AccessType: {course.AccessType}).");
                }

                if (course.Price > 0)
                {
                    amount = course.Price;
                }
            }
        }

        if (amount <= 0)
        {
            throw new BusinessRuleException("Order amount must be greater than zero for paid courses.");
        }

        var order = Order.Create(
            userId,
            command.CourseId,
            amount,
            currency);

        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var paymentUrl = $"{_settings.PaymentGatewayCheckoutUrl}?orderId={order.Id}&amount={order.Amount}&currency={order.Currency}";
        var responseDto = order.Adapt<CheckoutResponseDto>() with
        {
            OrderId = order.Id,
            PaymentUrl = paymentUrl
        };

        return ApiResponse.Ok(responseDto, "Checkout initiated successfully.");
    }
}
