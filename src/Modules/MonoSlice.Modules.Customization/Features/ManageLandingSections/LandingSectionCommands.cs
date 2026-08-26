using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;
using MonoSlice.Modules.Customization.Domain.Models;
using MonoSlice.Modules.Customization.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Customization.Features.ManageLandingSections;

// 1. Get Landing Sections
public sealed record GetLandingSectionsQuery() : IQuery<ApiResponse<List<LandingSectionDto>>>;

public sealed class GetLandingSectionsQueryHandler : IQueryHandler<GetLandingSectionsQuery, ApiResponse<List<LandingSectionDto>>>
{
    private readonly CustomizationDbContext _dbContext;

    public GetLandingSectionsQueryHandler(CustomizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<List<LandingSectionDto>>> Handle(
        GetLandingSectionsQuery query,
        CancellationToken cancellationToken)
    {
        var sections = await _dbContext.LandingSections
            .AsNoTracking()
            .OrderBy(s => s.OrderIndex)
            .Select(s => new LandingSectionDto
            {
                Id = s.Id,
                SectionType = s.SectionType,
                Title = s.Title,
                Subtitle = s.Subtitle,
                OrderIndex = s.OrderIndex,
                IsActive = s.IsActive,
                ConfigJson = s.ConfigJson
            })
            .ToListAsync(cancellationToken);

        return ApiResponse.Ok(sections, "Landing sections retrieved successfully.");
    }
}

// 2. Create Landing Section
public sealed record CreateLandingSectionCommand(
    string SectionType,
    string? Title,
    string? Subtitle,
    int OrderIndex,
    bool IsActive,
    string ConfigJson) : ICommand<ApiResponse<Guid>>;

public sealed class CreateLandingSectionCommandHandler : ICommandHandler<CreateLandingSectionCommand, ApiResponse<Guid>>
{
    private const string CacheKey = "customization:public";
    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public CreateLandingSectionCommandHandler(CustomizationDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<Guid>> Handle(
        CreateLandingSectionCommand command,
        CancellationToken cancellationToken)
    {
        var section = LandingSection.Create(
            command.SectionType,
            command.Title,
            command.Subtitle,
            command.OrderIndex,
            command.IsActive,
            command.ConfigJson);

        await _dbContext.LandingSections.AddAsync(section, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CacheKey, cancellationToken);

        return ApiResponse.Ok(section.Id, "Landing section created successfully.", 201);
    }
}

// 3. Update Landing Section
public sealed record UpdateLandingSectionCommand(
    Guid Id,
    string? Title,
    string? Subtitle,
    int OrderIndex,
    bool IsActive,
    string ConfigJson) : ICommand<ApiResponse<bool>>;

public sealed class UpdateLandingSectionCommandHandler : ICommandHandler<UpdateLandingSectionCommand, ApiResponse<bool>>
{
    private const string CacheKey = "customization:public";
    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public UpdateLandingSectionCommandHandler(CustomizationDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        UpdateLandingSectionCommand command,
        CancellationToken cancellationToken)
    {
        var section = await _dbContext.LandingSections
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (section is null)
            return ApiResponse.Fail<bool>("Landing section not found.", 404);

        section.Update(command.Title, command.Subtitle, command.OrderIndex, command.IsActive, command.ConfigJson);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CacheKey, cancellationToken);

        return ApiResponse.Ok(true, "Landing section updated successfully.");
    }
}

// 4. Delete Landing Section
public sealed record DeleteLandingSectionCommand(Guid Id) : ICommand<ApiResponse<bool>>;

public sealed class DeleteLandingSectionCommandHandler : ICommandHandler<DeleteLandingSectionCommand, ApiResponse<bool>>
{
    private const string CacheKey = "customization:public";
    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public DeleteLandingSectionCommandHandler(CustomizationDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        DeleteLandingSectionCommand command,
        CancellationToken cancellationToken)
    {
        var section = await _dbContext.LandingSections
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);

        if (section is null)
            return ApiResponse.Fail<bool>("Landing section not found.", 404);

        _dbContext.LandingSections.Remove(section);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CacheKey, cancellationToken);

        return ApiResponse.Ok(true, "Landing section deleted successfully.");
    }
}

// 5. Reorder Landing Sections
public sealed record ReorderLandingSectionsCommand(List<Guid> SectionIds) : ICommand<ApiResponse<bool>>;

public sealed class ReorderLandingSectionsCommandHandler : ICommandHandler<ReorderLandingSectionsCommand, ApiResponse<bool>>
{
    private const string CacheKey = "customization:public";
    private readonly CustomizationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public ReorderLandingSectionsCommandHandler(CustomizationDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async ValueTask<ApiResponse<bool>> Handle(
        ReorderLandingSectionsCommand command,
        CancellationToken cancellationToken)
    {
        var sections = await _dbContext.LandingSections.ToListAsync(cancellationToken);

        for (int i = 0; i < command.SectionIds.Count; i++)
        {
            var id = command.SectionIds[i];
            var section = sections.FirstOrDefault(s => s.Id == id);
            if (section is not null)
            {
                section.SetOrderIndex(i + 1);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKey, cancellationToken);

        return ApiResponse.Ok(true, "Landing sections reordered successfully.");
    }
}
