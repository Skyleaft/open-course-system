using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.CreateDiscussionThread;

public static class CreateDiscussionThreadEndpoint
{
    public static void MapCreateDiscussionThreadEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/threads", async (
            CreateDiscussionThreadCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("CreateDiscussionThread")
        .WithSummary("Start a discussion thread on a course or lesson.")
        .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
