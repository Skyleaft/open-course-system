using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GetExamResult;

public static class GetExamResultEndpoint
{
    public static void MapGetExamResultEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/submissions/{submissionId:guid}/result", async (
                Guid submissionId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetExamResultQuery(submissionId);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetExamSubmissionResult")
            .WithSummary("View exam submission score, answers, and explanations")
            .RequireAuthorization();
    }
}
