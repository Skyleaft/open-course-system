using MonoSlice.Modules.Communications.Features.CreateAnnouncement;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;

namespace MonoSlice.Modules.Communications.Features.GetAnnouncements;

public sealed record GetAnnouncementsQuery(
    Guid? CourseId = null,
    bool IncludeGlobal = true) : IQuery<ApiResponse<IReadOnlyList<AnnouncementDto>>>;
