using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.GetDiscussionThread;

public static class GetDiscussionThreadByIdEndpoint
{
    public static void MapGetDiscussionThreadByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/threads/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDiscussionThreadByIdQuery(id), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetDiscussionThreadById")
        .WithSummary("Get discussion thread details including nested comments.");
    }
}
