using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.Admin.RedriveDeadLetter;

public static class RedriveDeadLetterEndpoint
{
    public static void MapRedriveDeadLetterEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dlq/{id:guid}/re-drive", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RedriveDeadLetterCommand(id), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("RedriveAssessmentDeadLetter")
        .WithSummary("Replay and re-process a failed dead letter grading event.")
        .RequireAuthorization();
    }
}
