using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;

public static class GetLiveCandidatesEndpoint
{
    public static void MapGetLiveCandidatesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/exams/{examId:guid}/live-candidates", async (
                Guid examId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetLiveCandidatesQuery(examId);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetLiveCandidates")
            .WithSummary("Retrieve real-time list of exam candidates and proctor monitoring metrics")
            .RequireAuthorization();
    }
}
