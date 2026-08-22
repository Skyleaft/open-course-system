using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.GetCourse;

public static class GetCourseEndpoint
{
    public static void MapGetCourseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new GetCourseQuery(id);
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GetCourseById")
            .WithSummary("Get course details and syllabus")
            .AllowAnonymous();
    }
}
