using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Customization.Features.UpdateSiteSetting;

public static class UpdateSiteSettingEndpoint
{
    public static void MapUpdateSiteSettingEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/admin/settings/{settingKey}", async (
            string settingKey,
            UpdateSettingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateSiteSettingCommand(settingKey, request.ValueJson, request.IsPublic);
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UpdateSiteSetting")
        .WithSummary("Update a specific site setting key with raw JSON payload.");
    }
}

public sealed record UpdateSettingRequest(string ValueJson, bool? IsPublic);
