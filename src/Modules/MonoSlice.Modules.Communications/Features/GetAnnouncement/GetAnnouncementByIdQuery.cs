using MonoSlice.Modules.Communications.Features.CreateAnnouncement;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.GetAnnouncement;

public sealed record GetAnnouncementByIdQuery(Guid Id) : IQuery<ApiResponse<AnnouncementDto>>;
