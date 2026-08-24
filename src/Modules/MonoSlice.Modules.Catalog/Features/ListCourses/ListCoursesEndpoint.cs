using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Catalog.Features.ListCourses;

public static class ListCoursesEndpoint
{
    public static void MapListCoursesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", async (
                [AsParameters] ListCoursesQuery query,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(query, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("ListCourses")
            .WithSummary("List courses with advanced filter, sorting, and pagination")
            .AllowAnonymous();
    }
}
