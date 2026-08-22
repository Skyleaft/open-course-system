using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.CreateAnnouncement;

public static class CreateAnnouncementEndpoint
{
    public static void MapCreateAnnouncementEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/announcements", async (
            CreateAnnouncementCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("CreateAnnouncement")
        .WithSummary("Create a platform or course announcement.")
        .RequireAuthorization();
    }
}
