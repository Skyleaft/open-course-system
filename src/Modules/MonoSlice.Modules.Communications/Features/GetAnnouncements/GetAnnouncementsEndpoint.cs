using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.GetAnnouncements;

public static class GetAnnouncementsEndpoint
{
    public static void MapGetAnnouncementsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/announcements", async (
            Guid? courseId,
            bool? includeGlobal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAnnouncementsQuery(courseId, includeGlobal ?? true);
            var result = await sender.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetAnnouncements")
        .WithSummary("Query announcements with filter by course.");
    }
}
