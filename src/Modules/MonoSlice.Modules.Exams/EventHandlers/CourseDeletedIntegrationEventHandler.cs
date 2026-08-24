using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Exams.EventHandlers;

public sealed class CourseDeletedIntegrationEventHandler : IIntegrationEventHandler<CourseDeletedIntegrationEvent>
{
    private readonly ILogger<CourseDeletedIntegrationEventHandler> _logger;

    public CourseDeletedIntegrationEventHandler(ILogger<CourseDeletedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(CourseDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "CourseDeletedIntegrationEvent received for Course {CourseId}. Exams are decoupled and remain in QuestionBank & Exam repository.",
            @event.CourseId);

        return Task.CompletedTask;
    }
}
