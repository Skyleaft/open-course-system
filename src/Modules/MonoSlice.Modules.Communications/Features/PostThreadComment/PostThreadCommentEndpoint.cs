using System.ComponentModel.DataAnnotations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Communications.Features.PostThreadComment;

public sealed record PostCommentRequest(
    Guid? ParentCommentId,
    [Required] string Content);

public static class PostThreadCommentEndpoint
{
    public static void MapPostThreadCommentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/threads/{id:guid}/comments", async (
            Guid id,
            [FromBody] PostCommentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new PostThreadCommentCommand
            {
                ThreadId = id,
                ParentCommentId = request.ParentCommentId,
                Content = request.Content
            };

            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("PostThreadComment")
        .WithSummary("Post a top-level comment or nested reply to a discussion thread.")
        .RequireAuthorization(policy => policy.RequireRole("Student", "Instructor", "Admin"));
    }
}
