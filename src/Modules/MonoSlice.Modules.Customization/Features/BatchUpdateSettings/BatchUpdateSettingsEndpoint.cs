using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Customization.Features.BatchUpdateSettings;

public static class BatchUpdateSettingsEndpoint
{
    public static void MapBatchUpdateSettingsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/admin/batch", async (
            BatchUpdateSettingsCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("BatchUpdateSettings")
        .WithSummary("Update whole customization sections (Branding, Theme, Features, Localization, Security) in one batch.");
    }
}
