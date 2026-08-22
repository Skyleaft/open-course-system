using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.Admin.GetDeadLetters;

public static class GetDeadLettersEndpoint
{
    public static void MapGetDeadLettersEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dlq", async (
            bool? onlyUnresolved,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDeadLettersQuery(onlyUnresolved), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetAssessmentsDeadLetters")
        .WithSummary("Query failed and poison grading stream messages from DLQ.")
        .RequireAuthorization();
    }
}
