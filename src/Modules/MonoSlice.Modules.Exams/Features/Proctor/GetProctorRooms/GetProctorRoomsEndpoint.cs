using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Exams.Features.Proctor.GetProctorRooms;

public static class GetProctorRoomsEndpoint
{
    public static void MapGetProctorRoomsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/rooms", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetProctorRoomsQuery();
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetProctorRooms")
        .WithSummary("Get active examination proctoring rooms grouped by course")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor", "Proctor"));
    }
}
