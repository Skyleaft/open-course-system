using Mapster;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Communications.Features.CreateAnnouncement;

public sealed class CreateAnnouncementCommandHandler : ICommandHandler<CreateAnnouncementCommand, ApiResponse<AnnouncementDto>>
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateAnnouncementCommandHandler(
        CommunicationsDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<AnnouncementDto>> Handle(
        CreateAnnouncementCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to create an announcement.");
        }

        var announcement = Announcement.Create(
            command.CourseId,
            _currentUser.UserId.Value,
            command.Title,
            command.Content,
            command.IsPinned);

        await _dbContext.Announcements.AddAsync(announcement, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = announcement.Adapt<AnnouncementDto>();
        return ApiResponse.Ok(dto, "Announcement created successfully.");
    }
}
