using Mediator;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Shared.Abstractions.Domain;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Shared.Infrastructure.Persistence;

public abstract class BaseDbContext : DbContext, IUnitOfWork
{
    private readonly IMediator? _mediator;

    protected BaseDbContext(DbContextOptions options, IMediator? mediator = null)
        : base(options)
    {
        _mediator = mediator;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();

        var domainEvents = ExtractAndClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (_mediator is not null && domainEvents.Count > 0)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }

        return result;
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                var createdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdProp is not null && (createdProp.CurrentValue is null || (DateTime)createdProp.CurrentValue == default))
                {
                    createdProp.CurrentValue = utcNow;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                var updatedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedProp is not null)
                {
                    updatedProp.CurrentValue = utcNow;
                }
            }
        }
    }

    private List<IDomainEvent> ExtractAndClearDomainEvents()
    {
        var domainEventEntities = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IAggregateRoot>()
            .ToList();

        var events = new List<IDomainEvent>();

        foreach (var aggregate in domainEventEntities)
        {
            if (aggregate.DomainEvents.Count > 0)
            {
                events.AddRange(aggregate.DomainEvents);
                aggregate.ClearDomainEvents();
            }
        }

        return events;
    }
}
