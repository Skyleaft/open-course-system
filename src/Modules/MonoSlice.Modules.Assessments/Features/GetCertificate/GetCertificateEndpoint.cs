using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Assessments.Features.GetCertificate;

public static class GetCertificateEndpoint
{
    public static void MapGetCertificateEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{certificateNumber}", async (
            string certificateNumber,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetCertificateQuery(certificateNumber), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetCertificate")
        .WithSummary("Get certificate details by certificate number.")
        .AllowAnonymous();
    }
}
