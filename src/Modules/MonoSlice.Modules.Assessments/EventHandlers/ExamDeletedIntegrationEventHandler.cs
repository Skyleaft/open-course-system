using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;

namespace MonoSlice.Modules.Assessments.EventHandlers;

public sealed class ExamDeletedIntegrationEventHandler : IIntegrationEventHandler<ExamDeletedIntegrationEvent>
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ExamDeletedIntegrationEventHandler> _logger;

    public ExamDeletedIntegrationEventHandler(
        AssessmentsDbContext dbContext,
        ICacheService cacheService,
        ILogger<ExamDeletedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task HandleAsync(ExamDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing cascading cleanup for deleted exam {ExamId}", @event.ExamId);

        var gradeRecords = await _dbContext.GradeRecords
            .Where(g => g.ReferenceId == @event.ExamId && g.ItemType == GradeItemType.Quiz)
            .ToListAsync(cancellationToken);

        if (gradeRecords.Count > 0)
        {
            _dbContext.GradeRecords.RemoveRange(gradeRecords);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} grade records for deleted exam {ExamId}", gradeRecords.Count, @event.ExamId);
        }

        await _cacheService.RemoveAsync($"exam:grades:{@event.ExamId}", cancellationToken);
    }
}
