using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.GetStudentExamOverview;

public static class GetStudentExamOverviewEndpoint
{
    public static void MapGetStudentExamOverviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{id:guid}/overview", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetStudentExamOverviewQuery(id);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetStudentExamOverview")
            .WithSummary("Get sanitized public/student examination overview and attempt history metadata without leaking test questions or answers.")
            .AllowAnonymous();
    }
}
