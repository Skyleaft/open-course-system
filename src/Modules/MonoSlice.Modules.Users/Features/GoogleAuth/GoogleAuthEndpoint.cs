using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MonoSlice.Modules.Users.Features.GoogleAuth;

public static class GoogleAuthEndpoint
{
    public static void MapGoogleAuthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/google", async (
                GoogleAuthCommand command,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var response = await mediator.Send(command, ct);
                return Results.Json(response, statusCode: response.StatusCode);
            })
            .WithName("GoogleAuth")
            .WithSummary("Authenticate or register user using Google OAuth ID Token")
            .AllowAnonymous();
    }
}
