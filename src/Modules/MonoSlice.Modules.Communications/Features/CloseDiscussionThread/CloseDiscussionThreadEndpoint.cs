using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.CloseDiscussionThread;

public static class CloseDiscussionThreadEndpoint
{
    public static void MapCloseDiscussionThreadEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/threads/{id:guid}/close", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CloseDiscussionThreadCommand(id), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("CloseDiscussionThread")
        .WithSummary("Close a discussion thread to prevent further comments.")
        .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
