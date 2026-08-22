using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.IssueCertificate;

public static class IssueCertificateEndpoint
{
    public static void MapIssueCertificateEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/issue", async (
            IssueCertificateCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("IssueCertificate")
        .WithSummary("Manually issue a course completion certificate.")
        .RequireAuthorization();
    }
}
