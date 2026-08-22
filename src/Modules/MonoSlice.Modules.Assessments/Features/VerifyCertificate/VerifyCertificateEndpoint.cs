using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.VerifyCertificate;

public static class VerifyCertificateEndpoint
{
    public static void MapVerifyCertificateEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/verify/{certificateHash}", async (
            string certificateHash,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifyCertificateQuery(certificateHash), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("VerifyCertificate")
        .WithSummary("Public verification of certificate authenticity via cryptographic SHA-256 hash.")
        .AllowAnonymous();
    }
}
