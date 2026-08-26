using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Customization.Features.GetPublicCustomization;

public static class GetPublicCustomizationEndpoint
{
    public static void MapGetPublicCustomizationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/public", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPublicCustomizationQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("GetPublicCustomization")
        .WithSummary("Retrieve public website settings, theme tokens, and landing sections.");
    }
}
