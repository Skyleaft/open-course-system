using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Customization.Features.GetAdminCustomization;

public static class GetAdminCustomizationEndpoint
{
    public static void MapGetAdminCustomizationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin", async (
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAdminCustomizationQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetAdminCustomization")
        .WithSummary("Retrieve all website settings (including sensitive/security) for administration.");
    }
}
