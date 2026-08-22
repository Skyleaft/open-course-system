using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.GetAnnouncement;

public static class GetAnnouncementByIdEndpoint
{
    public static void MapGetAnnouncementByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/announcements/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAnnouncementByIdQuery(id), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetAnnouncementById")
        .WithSummary("Get announcement by ID.");
    }
}
