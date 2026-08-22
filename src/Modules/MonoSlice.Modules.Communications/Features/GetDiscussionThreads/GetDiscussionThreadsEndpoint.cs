using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.GetDiscussionThreads;

public static class GetDiscussionThreadsEndpoint
{
    public static void MapGetDiscussionThreadsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/threads", async (
            Guid? courseId,
            Guid? lessonId,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDiscussionThreadsQuery(courseId, lessonId, pageNumber ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetDiscussionThreads")
        .WithSummary("List discussion threads with optional course/lesson filters and pagination.");
    }
}
