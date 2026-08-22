using Mapster;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Features.CreateAnnouncement;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;

namespace MonoSlice.Modules.Communications.Features.GetAnnouncement;

public sealed class GetAnnouncementByIdQueryHandler : IQueryHandler<GetAnnouncementByIdQuery, ApiResponse<AnnouncementDto>>
{
    private readonly CommunicationsDbContext _dbContext;

    public GetAnnouncementByIdQueryHandler(CommunicationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ApiResponse<AnnouncementDto>> Handle(
        GetAnnouncementByIdQuery query,
        CancellationToken cancellationToken)
    {
        var announcement = await _dbContext.Announcements
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken);

        if (announcement is null)
        {
            throw new NotFoundException("Announcement", query.Id);
        }

        var dto = announcement.Adapt<AnnouncementDto>();
        return ApiResponse.Ok(dto);
    }
}
