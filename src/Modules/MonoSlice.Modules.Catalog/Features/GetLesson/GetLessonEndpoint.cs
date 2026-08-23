using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.GetLesson;

public static class GetLessonEndpoint
{
    public static void MapGetLessonEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/lessons/{lessonId:guid}", async (
                Guid lessonId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetLessonQuery(lessonId);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetLesson")
            .WithSummary("Get a lesson by its identifier")
            .RequireAuthorization();
    }
}
