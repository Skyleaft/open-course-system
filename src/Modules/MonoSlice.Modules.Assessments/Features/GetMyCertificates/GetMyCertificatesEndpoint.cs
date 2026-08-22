using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.GetMyCertificates;

public static class GetMyCertificatesEndpoint
{
    public static void MapGetMyCertificatesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/my-certificates", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetMyCertificatesQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetMyCertificates")
        .WithSummary("List all certificates issued to the current student.")
        .RequireAuthorization();
    }
}
