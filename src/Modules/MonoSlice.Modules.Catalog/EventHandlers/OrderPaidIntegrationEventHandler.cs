using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Catalog.EventHandlers;

public sealed class OrderPaidIntegrationEventHandler : IIntegrationEventHandler<OrderPaidIntegrationEvent>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ILogger<OrderPaidIntegrationEventHandler> _logger;

    public OrderPaidIntegrationEventHandler(
        CoursesDbContext dbContext,
        ILogger<OrderPaidIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPaidIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Consuming OrderPaidIntegrationEvent for Student {UserId} in Course {CourseId} (Order {OrderId}).",
            @event.UserId, @event.CourseId, @event.OrderId);

        var existing = await _dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == @event.UserId && e.CourseId == @event.CourseId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation("Student {UserId} is already enrolled in Course {CourseId}.", @event.UserId, @event.CourseId);
            return;
        }

        var enrollment = CourseEnrollment.Create(@event.UserId, @event.CourseId);
        await _dbContext.Enrollments.AddAsync(enrollment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully auto-enrolled Student {UserId} in Course {CourseId}.", @event.UserId, @event.CourseId);
    }
}
