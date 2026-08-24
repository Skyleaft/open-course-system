using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Features.CreateAnnouncement;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.GetAnnouncements;

public sealed class GetAnnouncementsQueryHandler : IQueryHandler<GetAnnouncementsQuery, ApiResponse<IReadOnlyList<AnnouncementDto>>>
{
    private readonly CommunicationsDbContext _dbContext;

    public GetAnnouncementsQueryHandler(CommunicationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<IReadOnlyList<AnnouncementDto>>> Handle(
        GetAnnouncementsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Announcements.AsNoTracking();

        if (query.CourseId.HasValue)
        {
            if (query.IncludeGlobal)
            {
                dbQuery = dbQuery.Where(a => a.CourseId == query.CourseId.Value || a.CourseId == null);
            }
            else
            {
                dbQuery = dbQuery.Where(a => a.CourseId == query.CourseId.Value);
            }
        }

        var announcements = await dbQuery
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = announcements.Adapt<List<AnnouncementDto>>();
        return ApiResponse.Ok<IReadOnlyList<AnnouncementDto>>(dtos, "Announcements retrieved successfully.");
    }
}
