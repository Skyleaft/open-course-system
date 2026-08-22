using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Orders.Domain.Events;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Orders.EventHandlers;

public sealed class OrderPaidDomainEventHandler : INotificationHandler<OrderPaidDomainEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderPaidDomainEventHandler> _logger;

    public OrderPaidDomainEventHandler(
        IServiceProvider serviceProvider,
        ILogger<OrderPaidDomainEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async ValueTask Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OrderPaidDomainEvent received for Order {OrderId}. Initiating auto-enrollment for Student {UserId} in Course {CourseId}.",
            notification.OrderId, notification.UserId, notification.CourseId);

        var coursesApi = _serviceProvider.GetService<ICoursesModuleApi>();
        if (coursesApi is not null)
        {
            var enrolled = await coursesApi.EnrollStudentAsync(
                notification.UserId,
                notification.CourseId,
                cancellationToken);

            if (enrolled)
            {
                _logger.LogInformation(
                    "Auto-enrollment completed for Student {UserId} in Course {CourseId}.",
                    notification.UserId, notification.CourseId);
            }
            else
            {
                _logger.LogWarning(
                    "Auto-enrollment returned false for Student {UserId} in Course {CourseId}.",
                    notification.UserId, notification.CourseId);
            }
        }
    }
}
